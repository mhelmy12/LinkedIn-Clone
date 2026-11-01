# Contract: Kafka Events (v1)

**Broker**: Aspire `kafka` resource (local `localhost:9094` via AppHost; compose
`localhost:9092`) · **CDC**: Debezium PostgreSQL connector for `chatDb`
**Spec**: [../spec.md](../spec.md) · **Plan**: [../plan.md](../plan.md)

Chat messages **never** traverse Kafka (FR-015). Kafka carries only projection
inputs (from other services) and content-free notification jobs (from
ChatService's outbox).

All payloads are wrapped in the standard Debezium envelope:

```json
{
  "before": null,
  "after": {
    "Id": "<uuid>",
    "Type": "ChatMessageNotificationRequested",
    "AggregateType": "Message",
    "AggregateId": "9876543210999",
    "Payload": "<json string>",
    "Timestamp": "2026-11-01T10:05:00.123Z"
  },
  "op": "c",
  "source": { "db": "chatDb", "table": "OutboxMessages" }
}
```

Consumers deserialize `after.Payload` only; `before`/`source` metadata must not
drive business logic.

## Consumed events (projection inputs)

Source topics are produced by the owning services through their own outboxes
(existing behavior). ChatService uses independent consumer groups and upserts
(Constitution III — idempotent by natural key; no inbox needed for pure upserts).

| Event | Source | Consumer group | ChatService effect |
|---|---|---|---|
| `ConnectionAccepted` | User Service outbox → Debezium | `chat-service-connection-accepted` | Upsert `ChatConnectionProjection` rows for both directions with `IsActive=true` (payload: `UserId1`, `UserId2`, `AcceptedAt`) |
| `ConnectionRemoved` | User Service outbox → Debezium | `chat-service-connection-removed` | Upsert `IsActive=false` for both directions (payload: `UserId1`, `UserId2`, `RemovedAt`) |
| `UserProfileUpdated` | User Service outbox → Debezium | `chat-service-user-profile-updated` | Upsert `ChatUserProjection` (`UserId`, `DisplayName = FirstName + LastName`, `Email`, `UpdatedAt`) |

Notes:
- User ids in these payloads are Keycloak `sub` strings and are stored as-is;
  no numeric conversion happens (2026-11-01 decision).
- Event DTOs are copied into `ChatService/Events/` per the repository's
  per-service copy convention; changes are additive-only (Constitution V).
- `ConnectionAccepted` only exists after a connection request is accepted;
  a projection miss triggers a one-shot UserService API check (research R5).

## Produced events (outbox → Debezium → Kafka)

### `ChatMessageNotificationRequested` v1

Written by `NotificationRequestService` in the **same transaction** as the
`Message` insert when the recipient is offline (FR-014/FR-015).

`Payload` JSON:

```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "messageId": "9876543210999",
  "conversationId": "9876543210987",
  "senderUserId": "3f6a1b2c-9d4e-4a7b-8c1d-2e5f6a7b8c9d",
  "recipientUserId": "7c8d9e0f-1a2b-4c3d-9e4f-5a6b7c8d9e0f",
  "occurredAt": "2026-11-01T10:05:00Z",
  "schemaVersion": 1
}
```

- **No message content, ciphertext, nonce, or preview** — only routing metadata
  (FR-021). Notification text is rendered from `ChatUserProjection.DisplayName`
  at dispatch time.
- Topic: default Debezium PostgreSQL topic for the chat connector, configured as
  `<topic.prefix>.public.OutboxMessages` (e.g.
  `chatService.public.OutboxMessages`). The database name is not part of the
  PostgreSQL connector's default topic naming convention. The connector JSON ships in
  `docker-compose`/infra config alongside the existing connectors.
- Consumer: `ChatNotificationConsumer`, group `chat-notification-worker`,
  `AutoOffsetReset=Earliest`, manual commit after success.
- Idempotency: `InboxMessage(ConsumerGroup, EventId)` unique key; duplicate
  deliveries are ignored (Constitution III).
- Retry policy: 3 attempts with exponential backoff (250 ms → 1 s → 4 s); then
  publish to `chat-notification-dlq` and commit so the partition advances
  (research R8). Failures never touch the already-persisted message.
- Delivery: email on the recipient's `ChatUserProjection.Email` and push via
  `IChatPushSender` when the channel is enabled (research R14); per-channel
  outcomes are logged with the event id.

### `ChatMessageNotificationDelivered` (optional, v1)

Emitted best-effort after dispatch for observability. Additive, not consumed by
any service yet; included so audit/notification features can attach without a
breaking change. Same outbox table.

## Compatibility rules

- Additive fields only; never remove/rename/retype within v1 (Constitution V).
- New incompatible shapes get a new `Type`/`schemaVersion` and a migration
  window; deprecated fields stay until consumers stop depending on them.
- Consumer groups remain per-service/per-purpose (`chat-service-*`,
  `chat-notification-worker`); offsets are independent per group.
