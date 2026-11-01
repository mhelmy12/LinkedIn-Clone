# Contract: SignalR Hub (v1)

**Path**: `/hubs/chat` (exposed through the gateway as `wss://<gateway>/hubs/chat`)
**Transport**: WebSockets first, SignalR fallbacks (SSE/Long Polling)
**Scale-out**: Redis backplane (Aspire `redis` resource); no sticky sessions required
**Spec**: [../spec.md](../spec.md) · **Plan**: [../plan.md](../plan.md) · **REST**: [rest-api.md](./rest-api.md)

## Connection & authentication

- The API Gateway authenticates the connection with the Keycloak session cookie
  and YARP injects `X-User-Id` (the Keycloak `sub`) on the forwarded
  negotiate/WebSocket request. ChatService maps that header to a SignalR
  principal through a `GatewayHeader` authentication scheme; it does not
  validate JWTs itself (research R2).
- The gateway strips `Cookie` before forwarding, so ChatService never sees the
  session. Direct-to-service calls in local tests must supply `X-User-Id`;
  production ingress is the gateway only.
- CORS is configured for the client origins list (`Chat:Cors:Origins`);
  credentials allowed. The repository has no CORS elsewhere — this is scoped to
  ChatService only.
- One connection may watch multiple conversations; membership in SignalR groups
  is derived from the caller's conversation list.

### Groups

| Group | Membership | Used for |
|---|---|---|
| `user:{userId}` | the user's connections | read receipts to sender, presence notifications |
| `conv:{conversationId}` | both participants | `MessageReceived`, `TypingChanged` |
| `presence:{userId}` | users watching that user | `PresenceChanged` |

## Client → server methods

### `SendMessage`

```json
{
  "conversationId": "9876543210987",
  "clientMessageId": "6f9619ff-8b86-4f26-b7a6-4a2c7d3a6e11",
  "ciphertext": "<base64>",
  "nonce": "<base64>",
  "algorithm": "AES-256-GCM-v1"
}
```

Server behavior (single EF transaction):
1. Validate caller is a participant and still an active connection (FR-001).
2. Dedupe on `(conversationId, senderId, clientMessageId)`; reuse existing
   message on retry (FR-019/FR-023).
3. Persist ciphertext as-is; advance `Conversation.LastMessageAt`.
4. If the recipient is offline, append the content-free outbox notification
   request in the same transaction (FR-014/FR-015).
5. Broadcast `MessageReceived` to `conv:{conversationId}` (both replicas via the
   backplane) and return `MessageSentAck` to the caller.

**Ack** (hub return value):

```json
{
  "clientMessageId": "6f9619ff-8b86-4f26-b7a6-4a2c7d3a6e11",
  "messageId": "9876543210999",
  "sentAt": "2026-11-01T10:05:00Z",
  "duplicate": false
}
```

Errors are thrown as `HubException` with a stable code in the message:
`NOT_CONNECTED`, `NOT_PARTICIPANT`, `INVALID_ENVELOPE`, `PAYLOAD_TOO_LARGE`.
Clients keep the message in a **failed** state with explicit retry (FR-023);
retry reuses the same `clientMessageId`.

### `MarkRead`

```json
{ "conversationId": "9876543210987", "upToMessageId": "9876543210999" }
```

Same semantics as `POST .../read`; broadcast `ReadReceiptUpdated`.

### `SetTyping`

```json
{ "conversationId": "9876543210987", "isTyping": true }
```

- Server throttles per connection (at most one event per ~2 s), keeps a 5 s TTL
  key in Redis, broadcasts `TypingChanged` to `conv:{conversationId}` except
  the sender. No persistence (FR-008/SC-002).

### `WatchPresence`

```json
{ "userIds": ["7c8d9e0f-1a2b-4c3d-9e4f-5a6b7c8d9e0f"] }
```

Joins `presence:{userId}` for connected peers only and immediately emits their
current state. Unwatch on disconnect (group membership is connection-scoped).

## Server → client events

### `MessageReceived`

```json
{
  "id": "9876543210999",
  "conversationId": "9876543210987",
  "senderId": "3f6a1b2c-9d4e-4a7b-8c1d-2e5f6a7b8c9d",
  "clientMessageId": "6f9619ff-8b86-4f26-b7a6-4a2c7d3a6e11",
  "ciphertext": "<base64>",
  "nonce": "<base64>",
  "algorithm": "AES-256-GCM-v1",
  "sentAt": "2026-11-01T10:05:00Z"
}
```

### `TypingChanged`

```json
{ "conversationId": "9876543210987", "userId": "3f6a1b2c-9d4e-4a7b-8c1d-2e5f6a7b8c9d", "isTyping": true }
```

Sent with `isTyping:false` when TTL expiry is detected or the user leaves.

### `PresenceChanged`

```json
{ "userId": "3f6a1b2c-9d4e-4a7b-8c1d-2e5f6a7b8c9d", "online": false, "occurredAt": "2026-11-01T10:06:00Z" }
```

Emitted on connect/disconnect transitions (debounced by the Redis connection
set); SC-003 tolerances apply.

### `ReadReceiptUpdated`

```json
{
  "conversationId": "9876543210987",
  "readerId": "7c8d9e0f-1a2b-4c3d-9e4f-5a6b7c8d9e0f",
  "upToMessageId": "9876543210999",
  "readAtUtc": "2026-11-01T10:07:00Z"
}
```

## Lifecycle behavior

| Event | Server action |
|---|---|
| Connect | Add connection id to `chat:presence:{userId}`; emit `PresenceChanged(true)` to watchers; auto-join `user:{userId}` and `conv:{id}` groups for the caller's conversations |
| Disconnect | Remove connection id; when the set empties, emit `PresenceChanged(false)`; clear typing keys owned by the connection; leave groups |
| Reconnect | Client re-fetches missed history via REST `before` paging, then resumes live events (at-least-once backplane semantics make client-side dedupe by message id required) |
| Backplane failure | Logged + surfaced via health/metrics; local delivery continues (graceful degradation) |

## Versioning

Hub method/event payloads are additive-only within v1. Breaking changes require
a new hub path (`/hubs/chat/v2`) or a new method name; deprecated fields remain
until clients migrate (Constitution V applies to event contracts; the hub
follows the same discipline).
