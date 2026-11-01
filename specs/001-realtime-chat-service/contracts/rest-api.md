# Contract: Chat REST API (v1)

**Service**: `chat-service` · **Gateway prefix**: `/api/chat/**` (YARP route
`chat-service-route`) · **Spec**: [../spec.md](../spec.md) · **Plan**: [../plan.md](../plan.md)

## Conventions

- JSON over HTTPS; timestamps ISO-8601 UTC; Snowflake `long` ids (Conversation,
  Message) serialized as JSON **strings** per the repo convention.
- Caller identity: the gateway injects `X-User-Id` (Keycloak `sub`); ChatService
  uses that value directly as the user identity. User ids in payloads are
  Keycloak subjects (opaque strings), never numeric.
- Direct-to-service calls in local tests must supply `X-User-Id`; production
  ingress is the gateway only.
- Success bodies use the shared `Response<T>` envelope; errors use
  `GlobalExceptionHandler` semantics (400 invalid operation, 422 validation,
  403 not a participant/not connected, 404 unknown conversation, 500 generic).
- Idempotency: message creation is keyed by `clientMessageId`; conversation
  creation is keyed by the normalized participant pair.
- Rate limiting: gateway `user-or-ip` policy (100/min) already applies to
  `/api/chat/**`.

## Endpoints

### `POST /api/chat/conversations`

Create or return the single conversation between the caller and a connection.

```json
{ "participantUserId": "3f6a1b2c-9d4e-4a7b-8c1d-2e5f6a7b8c9d" }
```

- `200 OK` with existing conversation, `201 Created` when newly created.
- `403` when the pair is not an active connection (FR-002).
- Response:

```json
{
  "id": "9876543210987",
  "participant1Id": "3f6a1b2c-9d4e-4a7b-8c1d-2e5f6a7b8c9d",
  "participant2Id": "7c8d9e0f-1a2b-4c3d-9e4f-5a6b7c8d9e0f",
  "createdAt": "2026-11-01T10:00:00Z",
  "lastMessageAt": "2026-11-01T10:00:00Z"
}
```

### `GET /api/chat/conversations?before={lastMessageAt}&limit=50`

Conversation list ordered by `lastMessageAt DESC` (FR-013). `limit` default 50,
max 100. Each item adds `peer` summary (`userId`, `displayName`) and
`unreadCount` (messages sent by the peer with `readAtUtc == null`).

### `GET /api/chat/conversations/{conversationId}`

Single conversation with both participant summaries and current presence of the
peer (`online` boolean).

### `GET /api/chat/conversations/{conversationId}/messages?before={messageId}&limit=50`

Keyset history page, newest → oldest (FR-022). `limit` default 50, max 100.

```json
{
  "items": [
    {
      "id": "9876543210999",
      "senderId": "3f6a1b2c-9d4e-4a7b-8c1d-2e5f6a7b8c9d",
      "clientMessageId": "6f9619ff-8b86-4f26-b7a6-4a2c7d3a6e11",
      "ciphertext": "<base64>",
      "nonce": "<base64>",
      "algorithm": "AES-256-GCM-v1",
      "sentAt": "2026-11-01T10:05:00Z",
      "readAtUtc": null
    }
  ],
  "hasMore": true
}
```

- `403` when the caller is not a participant; `404` unknown conversation.
- The server never returns plaintext — clients decrypt locally (FR-021).

### `POST /api/chat/conversations/{conversationId}/messages`

Durable send/retry fallback for the hub (FR-023). Same validation and dedupe as
the hub `SendMessage`.

```json
{
  "clientMessageId": "6f9619ff-8b86-4f26-b7a6-4a2c7d3a6e11",
  "ciphertext": "<base64>",
  "nonce": "<base64>",
  "algorithm": "AES-256-GCM-v1"
}
```

- `201` with the stored message (or `200` with the existing one when the
  `clientMessageId` was already persisted — idempotent retry).
- `403` not connected/not a participant; `422` malformed envelope.
- Offline recipient triggers the outbox notification request in the same
  transaction (FR-014/FR-015).

### `POST /api/chat/conversations/{conversationId}/read`

```json
{ "upToMessageId": "9876543210999" }
```

- `204`; bulk-sets `readAtUtc` on unread messages of the peer with `id <= upTo`.
- Broadcasts `ReadReceiptUpdated` to the original sender (FR-010/FR-011).

### `PUT /api/chat/devices/{deviceId}`

Register or refresh this device's public key.

```json
{ "publicKey": "<base64 SPKI>", "algorithm": "ECDH-P256-v1" }
```

- `204`. `deviceId` is a client-generated GUID stable per install.
- The private key never leaves the device (FR-021).

### `GET /api/chat/users/{userId}/devices`

Active (`revokedAt == null`) device public keys of a user the caller is
connected to — used to wrap the conversation key (research R3).

### `PUT /api/chat/conversations/{conversationId}/keys`

Upload wrapped conversation-key envelopes addressed to active devices.

```json
{
  "envelopes": [
    { "deviceId": "a1b2...", "wrappedKey": "<base64>", "nonce": "<base64>" }
  ]
}
```

- `204`; participant-only; every `deviceId` must belong to one of the two
  participants. Envelopes are opaque ciphertext to the server.

### `GET /api/chat/conversations/{conversationId}/keys`

Returns the caller's own device envelopes so a fresh device can unwrap the
conversation key and read history.

### `GET /api/chat/presence?userIds=1,2,3`

Presence snapshot for conversation partners (FR-009): `{ "1": true, "2": false }`.

## Failure semantics

| Case | Status |
|---|---|
| Caller not authenticated | 401 (gateway) |
| Caller is not a participant of the conversation | 403 |
| Pair is not an active connection | 403 |
| Conversation/message unknown or `upToMessageId` outside conversation | 404/422 |
| Envelope/nonce/algorithm invalid or ciphertext over 16 KiB | 422 |
| Duplicate `clientMessageId` | 200 (existing message, no duplicate created) |
| Validation errors (payload shape) | 422 |
