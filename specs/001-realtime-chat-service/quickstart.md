# Quickstart: Real-Time Chat & Messaging Service

**Feature**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md) · **Contracts**: [contracts/](./contracts/) · **Data model**: [data-model.md](./data-model.md)

This guide runs the ChatService end-to-end locally and validates the feature
against its measurable outcomes. It is a validation/run guide — implementation
details live in `tasks.md`.

## Prerequisites

- .NET 9 SDK, Docker Desktop (Linux containers), Git
- ~8 GB free memory for the full Aspire topology
- The repository builds: `dotnet restore "LinkedIn Clone.sln"`

## 1. Start the platform

```powershell
dotnet run --project AppHost/AppHost.csproj
```

Expected:
- Aspire dashboard lists `chat-service`, `chatDb` (PostgreSQL), `redis`, `kafka`.
- `chat-service` health endpoints are green (`/health`, `/alive` in Development).
- ChatService logs show the SignalR hub mapped at `/hubs/chat` and the Kafka
  notification consumer started.

Compose alternative: `docker-compose up` after adding the `postgres` service and
the Chat Debezium connector (see `docker-compose.yml`); keep it aligned with
the Aspire topology.

## 2. Test users and auth

Create two Keycloak users (realm import at `AppHost/Realms`) and accept a
connection between them through the User Service flow (`ConnectionAccepted`
must be published). Through the gateway, callers authenticate normally; for
direct service tests supply the Keycloak `sub` explicitly:

```powershell
# dev-only header the gateway normally injects
$headers = @{ "X-User-Id" = "<keycloak-sub-of-user-a>" }
$base = "https://localhost:<gateway-port>/api/chat"
```

Register each device's ECDH public key first
(`PUT $base/devices/{deviceId}` — see contracts/rest-api.md), then create the
conversation (`POST $base/conversations`).

## 3. Validation scenarios

Run these in order; each maps to acceptance criteria in the spec.

### V1 — Message exchange, ordering, history (FR-004–006, SC-001, SC-005, SC-007)

1. Connect both users' SignalR clients to `wss://<gateway>/hubs/chat` with the
   gateway session; the gateway injects `X-User-Id` (Keycloak `sub`) on the
   upgrade. Direct-to-service tests may set that header explicitly.
2. User A calls `SendMessage` with a client-generated `clientMessageId` and an
   AES-256-GCM ciphertext (the test helper `ChatCryptoTestClient` in
   `Tests/ChatService.IntegrationTests` does this).
3. Expected: B receives `MessageReceived` within 1 s; A receives
   `MessageSentAck` with a Snowflake `messageId`.
4. Resend the same `clientMessageId` → `duplicate: true`, no second message.
5. Fetch `GET $base/conversations/{id}/messages?limit=50` → newest 50 in
   descending `id`; `hasMore` reflects older pages; page back with
   `before=<oldest id>` until exhausted.
6. Restart `chat-service` and re-fetch → history unchanged (SC-005).

### V2 — Typing indicators (FR-008, SC-002)

With both clients watching the conversation, A calls `SetTyping true` →
B receives `TypingChanged(true)` within 1 s. A stops typing without another
event → B receives `TypingChanged(false)` within 5 s (Redis TTL). No typing
state is ever returned by history endpoints.

### V3 — Presence (FR-009, SC-003)

B calls `WatchPresence` for A. A connects → B receives `PresenceChanged(true)`
within ~2 s. A disconnects → B receives `PresenceChanged(false)` within ~30 s.
`GET $base/presence?userIds=<A>` returns `{"<A>": true|false}` consistently.

### V4 — Read receipts (FR-010–012, SC-004)

A sends a message while B's conversation is closed. B opens the conversation
and calls `MarkRead` with `upToMessageId` → A receives `ReadReceiptUpdated`
within 2 s; the REST message payload for that id now has a non-null
`readAtUtc`. Calling `MarkRead` again changes nothing (idempotent).

### V5 — Connection guard (FR-001/002, SC-010)

Attempt `POST $base/conversations` for a non-connected user → `403` with an
explanatory error, no conversation/message rows created. Remove the connection
between A and B, then attempt `SendMessage` → `HubException NOT_CONNECTED`;
history stays viewable for both.

### V6 — Offline notification pipeline (FR-014/015, SC-009)

With B fully disconnected, A sends a message. Expected:
- The message is persisted and delivered to B on reconnect.
- A row appears in `chatDb."OutboxMessages"` of type
  `ChatMessageNotificationRequested` containing no message content.
- Debezium publishes it; `ChatNotificationConsumer` logs email + push attempts
  (dev senders are logging no-ops when providers are disabled).
- Re-delivering the same event (restart the consumer with the offset reset)
  produces no duplicate notification — `InboxMessage` dedupe.
- Failure path: force the sender to fail 3× → event lands on
  `chat-notification-dlq`; message sending remains unaffected.

### V7 — End-to-end privacy (FR-021)

Query `chatDb."Messages"` → only `Ciphertext`, `Nonce`, `Algorithm` and metadata
exist. The notification outbox payload contains no content. A client without a
wrapped conversation key cannot decrypt the stored ciphertext.

### V8 — Horizontal scale (FR-007, SC-008)

Optional (requires two Snowflake worker ids): run two ChatService replicas
against the same Redis/PostgreSQL. Connect A to replica 1 and B to replica 2 →
messages, typing, presence and read receipts still flow end-to-end
(Redis backplane). Locally, enable two instances through the AppHost and set
`Chat__SnowflakeWorkerId` distinctly per instance.

## 4. Automated tests

```powershell
dotnet build "LinkedIn Clone.sln"
dotnet test Tests/ChatService.Tests
dotnet test Tests/ChatService.IntegrationTests
```

- Unit project: pair normalization/uniqueness race handling, envelope validation,
  mark-read-up-to semantics, projection upsert logic, Snowflake ordering.
- Integration project (Testcontainers PostgreSQL + Redis + Kafka,
  `WebApplicationFactory`, SignalR client): V1–V7 above, including restart
  recovery, duplicate delivery, DLQ routing, and multi-instance backplane
  delivery.

Expected: all green; `SC-001`–`SC-011` covered by the scenarios or their
automated equivalents.

## 5. Cleanup

Stop the Aspire session (Ctrl+C) and remove the `chatDb` volume data if a clean
schema is needed (`docker volume` for the `postgres` data volume).
