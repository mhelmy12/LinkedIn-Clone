# Tasks: Real-Time Chat & Messaging Service

**Input**: Design documents from `/specs/001-realtime-chat-service/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md
**Tests**: Included — the project constitution (Section 3) and `quickstart.md`
mandate integration coverage for consumers, projections, and persistence paths,
and `plan.md` lists the test projects. Test tasks are written test-first within
their phase.
**Organization**: Phased by user story. Each phase is a small, independently
reviewable increment with its own **review gate**.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US5 map to the prioritized user stories in `spec.md`
- Exact file paths are included in every task

## Phase Overview (tracking board)

| Phase | Scope | Tasks | Review gate |
|---|---|---|---|
| 1 | Setup — project, topology, gateway, compose | T001–T010 | service starts, health green |
| 2A | Foundational — data layer & migration | T011–T021 | migration applies, tables exist |
| 2B | Foundational — cross-service projections | T022–T028 | consumers start, events project |
| 2C | Foundational — realtime hub backbone & auth | T029–T034 | hub negotiates with valid token |
| 2D | Foundational — access control & serialization | T035–T038 | connection guard passes |
| 3 | **US1** Message exchange (MVP) | T039–T051 | quickstart V1 green |
| 4 | **US2** Typing indicators | T052–T055 | quickstart V2 green |
| 5 | **US3** Presence | T056–T059 | quickstart V3 green |
| 6 | **US4** Read receipts | T060–T063 | quickstart V4 green |
| 7 | **US5** Offline notifications | T064–T070 | quickstart V6 green |
| 8 | Polish & cross-cutting | T071–T076 | V7/V8 + full suite |

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Stand up an empty, wired ChatService so every later phase is a
reviewable diff inside a running system.

- [X] T001 Create `ChatService/ChatService.csproj` (net9.0, Nullable/ImplicitUsings, EF Core 9.0.19, Npgsql 9.0.4, EF Design/Tools, Confluent.Kafka 2.15.0, Aspire.StackExchange.Redis 13.5.3, `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, `MyBank.Shared.Core` 1.0.4, ProjectReference `ServiceDefaults`)
- [X] T002 Create `ChatService/Program.cs` skeleton: `AddServiceDefaults()`, `AddSharedInfrastructure(assembly)`, `AddDbContext<ChatDbContext>(UseNpgsql("ChatDbConnection"))`, `AddIdGen(5)`, `AddRedisClient("redis")`, `AddSignalR().AddStackExchangeRedis(...)`, `MapCarter()`, `MapHub<ChatHub>("/hubs/chat")`, `MapDefaultEndpoints()`
- [X] T003 [P] Register topology in `AppHost/Program.cs`: `postgres.AddDatabase("chatDb")` + `AddProject<ChatService>("chat-service").WithReference(chatDb, "ChatDbConnection").WithReference(redis).WithReference(kafka).WithReference(userService)`
- [X] T004 [P] Add gateway routes in `APIGateway/appsettings.json`: `chat-service-route` (`/api/chat/{**catch-all}`, PathRemovePrefix `/api`, auth `Default`, limiter `user-or-ip`) and `chat-hub-route` (`/hubs/chat/{**catch-all}` so negotiate/transport subpaths are included, no prefix transform, same cluster)
- [X] T005 [P] Add `ChatService/ChatService.csproj` to `LinkedIn Clone.sln` (root) and create the `Tests` solution-folder entries for the two test projects
- [X] T006 [P] Add a PostgreSQL service and the chat Debezium connector file to `docker-compose.yml` / `infra/debezium/chat-outbox-connector.json` for Aspire/compose parity
- [X] T007 [P] Create `ChatService/appsettings.json` and `ChatService/appsettings.Development.json` with `Chat:SnowflakeWorkerId=5`, `Chat:Cors:Origins`, `Chat:Push:Provider/Enabled`, `Kafka:BootstrapServers`
- [X] T008 [P] Create `ChatService/Properties/launchSettings.json` without fixed `applicationUrl` ports (Aspire proxies dynamic ports)
- [X] T009 [P] Create `Tests/ChatService.Tests/ChatService.Tests.csproj` (xUnit 2.9.3, NSubstitute, EF InMemory, references `ChatService` + `Tests/LinkedIn.Testing.Shared`)
- [X] T010 [P] Create `Tests/ChatService.IntegrationTests/ChatService.IntegrationTests.csproj` (Testcontainers PostgreSQL/Redis/Kafka, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.SignalR.Client`, references `ChatService`)

**Review gate**: `dotnet build "LinkedIn Clone.sln"` is green; the Aspire
dashboard shows `chat-service` + `chatDb`; `/health` and `/alive` respond;
gateway `/api/chat/...` returns 404 (service has no features yet) instead of a
routing error.

---

## Phase 2: Foundational (Blocking Prerequisites)

### Phase 2A — Data layer & migration (T011–T021)

- [X] T011 [P] Create `ChatService/Models/Conversation.cs` (`Id long` PK, `Participant1Id`/`Participant2Id` are Keycloak `sub` strings in ordinal lower/higher order, `CreatedAt`, `LastMessageAt`)
- [X] T012 [P] Create `ChatService/Models/Message.cs` (`Id long` PK, `ConversationId`, `SenderId` Keycloak `sub`, `ClientMessageId Guid`, `Ciphertext bytea`, `Nonce bytea(12)`, `Algorithm`, `SentAt`, `ReadAtUtc` nullable)
- [X] T013 [P] Create `ChatService/Models/ConversationKeyEnvelope.cs` (composite PK `(ConversationId, DeviceId)`, `WrappedKey`, `Nonce`, `CreatedAt`)
- [X] T014 [P] Create `ChatService/Models/UserDeviceKey.cs` (`DeviceId Guid` PK, `UserId` Keycloak `sub`, `PublicKey`, `Algorithm`, `RegisteredAt`, `RevokedAt` nullable)
- [X] T015 [P] Create `ChatService/Models/ChatUserProjection.cs` (`UserId` Keycloak `sub` PK, `DisplayName`, `Email`, `UpdatedAt`)
- [X] T016 [P] Create `ChatService/Models/ChatConnectionProjection.cs` (composite PK `(UserId, OtherUserId)`, `IsActive`, `UpdatedAt`)
- [X] T017 [P] Create `ChatService/Models/OutboxMessage.cs` (`Id Guid` PK, `Type`, `AggregateType`, `AggregateId`, `Payload jsonb`, `Timestamp`) and `ChatService/Models/InboxMessage.cs` (composite PK `(ConsumerGroup, EventId)`, `ProcessedAt`)
- [X] T018 [P] Create `ChatService/Data/ConversationConfiguration.cs` (unique index `(Participant1Id, Participant2Id)`, index `(LastMessageAt DESC)`) and `ChatService/Data/MessageConfiguration.cs` (unique `(ConversationId, SenderId, ClientMessageId)`, index `(ConversationId, Id DESC)`, partial unread index, ciphertext ≤ 16 KiB check)
- [X] T019 [P] Create EF configurations `ChatService/Data/ConversationKeyEnvelopeConfiguration.cs`, `UserDeviceKeyConfiguration.cs`, `ChatUserProjectionConfiguration.cs`, `ChatConnectionProjectionConfiguration.cs`, `OutboxMessageConfiguration.cs`, `InboxMessageConfiguration.cs` (all `ValueGeneratedNever()` where ids are client/queue supplied)
- [X] T020 Create `ChatService/Data/ChatDbContext.cs` with the eight `DbSet`s and `modelBuilder.ApplyConfigurationsFromAssembly(...)`
- [X] T021 Create the initial EF migration in `ChatService/Migrations/` and verify it applies to the Aspire `chatDb` (all tables + indexes present)

**Review gate**: open the migration diff — unique pair index, unique
`ClientMessageId` index, history index, partial unread index are all present;
`chatDb` tables match `data-model.md`.

### Phase 2B — Cross-service projections (T022–T028)

- [X] T022 [P] Create `ChatService/Helpers/KafkaConfiguration/KafkaOptions.cs` (`BootstrapServers`, `AutoOffsetReset=Earliest`, `EnableAutoCommit=false`)
- [X] T023 [P] Create event DTOs in `ChatService/Events/`: `ConnectionAcceptedEvent.cs`, `ConnectionRemovedEvent.cs`, `UserProfileUpdatedEvent.cs` (field names copied from the existing User Service payloads)
- [X] T024 Create `ChatService/Consumers/KafkaConsumerBase.cs` implementing manual commit, per-message DI scope, cancellation handling, bounded retry + DLQ hook (fixes the repo's infinite-retry pattern)
- [X] T025 [P] Create `ChatService/Consumers/ConnectionAcceptedConsumer.cs` (group `chat-service-connection-accepted`, upsert both directions `IsActive=true`)
- [X] T026 [P] Create `ChatService/Consumers/ConnectionRemovedConsumer.cs` (group `chat-service-connection-removed`, upsert both directions `IsActive=false`)
- [X] T027 [P] Create `ChatService/Consumers/UserProfileUpdatedConsumer.cs` (group `chat-service-user-profile-updated`, upsert `DisplayName`/`Email`)
- [X] T028 Register Kafka options + hosted consumers in `ChatService/Program.cs`

**Review gate**: with Kafka running, the three consumers start without
exceptions; a `ConnectionAccepted` event produces two projection rows;
re-delivering the same event changes nothing (upsert idempotency).

### Phase 2C — Realtime hub backbone & auth (T029–T034)

- [X] T029 [P] Create `ChatService/Hubs/ChatHubEvents.cs` with the server→client event name constants from `contracts/signalr-hub.md`
- [X] T030 Create `ChatService/Hubs/ChatHub.cs` skeleton with `OnConnectedAsync`/`OnDisconnectedAsync` and `user:{userId}` group membership
- [X] T031 [P] Create `ChatService/Services/CurrentChatUserResolver.cs` (Keycloak `sub` claim / `X-User-Id` used directly as the user identity; 401 when absent)
- [X] T032 [P] Create `ChatService/Infrastructure/ChatUserIdProvider.cs` (`IUserIdProvider` returning the Keycloak `sub`)
- [X] T033 Create `ChatService/Services/ChatGroupManager.cs` (join/leave `conv:{id}` groups for the caller's conversations)
- [X] T034 Finalize hub auth in `ChatService/Program.cs`: `GatewayHeader` authentication scheme bound to the gateway-injected `X-User-Id` (no JWT/`access_token` handling), Redis backplane binding, and the `Chat:Cors:Origins` CORS policy

**Review gate**: negotiate succeeds when the gateway `X-User-Id` header is
present and fails with 401 when it is missing; two clients on different app
instances (same Redis) both receive a test broadcast via a temporary echo
method (removed before Phase 3).

### Phase 2D — Access control & serialization (T035–T038)

- [X] T035 Create `ChatService/Services/IUserDirectoryClient.cs` + `UserDirectoryClient.cs` (typed `HttpClient` to `http://user-service` through Aspire service discovery, timeout + failure handling; uses `GET /users/profile/{userId}` and the new `GET /users/connections/{otherUserId}/status` fallback endpoint)
- [X] T036 [P] Create `ChatService/Services/PairNormalizer.cs` (`min/max` participant ordering used by every conversation write)
- [X] T037 Create `ChatService/Services/ConversationAccessService.cs` (participant check + active-connection check via `ChatConnectionProjection` with one-shot API verification and caching)
- [X] T038 [P] Create `ChatService/Infrastructure/LongToStringJsonConverter.cs` so Snowflake ids serialize as JSON strings per `contracts/rest-api.md`

**Review gate**: foundation complete — build green, migration applied, consumers
running, hub authenticated, access guard rejected for a non-connection, ids
serialized as strings in a sample response.

---

## Phase 3: User Story 1 — Exchange Messages with a Connection (Priority: P1) — MVP

**Goal**: Send/receive messages in real time, persist them durably, page
history, enforce connection-only messaging, and support retry without
duplicates.
**Independent Test**: two accounts, one conversation, messages arrive live,
history pages correctly after restart, duplicate `clientMessageId` is a no-op,
non-connections are rejected (quickstart V1 + V5).

### Tests for US1

- [ ] T039 [P] [US1] Unit tests in `Tests/ChatService.Tests/PairNormalizerTests.cs` and `Tests/ChatService.Tests/ConversationAccessTests.cs` (min/max ordering, non-connection rejection, API fallback cache)
- [ ] T040 [P] [US1] Create `Tests/ChatService.IntegrationTests/Fixtures/ChatTestFixture.cs` (Testcontainers PostgreSQL/Redis/Kafka + `WebApplicationFactory<Program>`) and `ChatCryptoTestClient.cs` (P-256 ECDH + AES-256-GCM helper)
- [ ] T041 [US1] Integration tests in `Tests/ChatService.IntegrationTests/MessageExchangeTests.cs`: live delivery < 1 s, duplicate `clientMessageId` dedupe, keyset paging, history survives service restart

### Implementation for US1

- [ ] T042 [P] [US1] Create `ChatService/Features/Conversations/CreateOrGetConversation/{Command,Endpoint,Handler,Response,Validation}.cs` (normalized pair, unique-violation retry-read, 403 for non-connections)
- [ ] T043 [P] [US1] Create `ChatService/Features/DeviceKeys/RegisterDeviceKey/...` and `ChatService/Features/DeviceKeys/GetPeerDevices/...` (register/refresh P-256 public key; list active peer device keys)
- [ ] T044 [US1] Create `ChatService/Features/DeviceKeys/PutConversationKeys/...` and `GetConversationKeys/...` (opaque per-device wrapped envelopes; participant-only)
- [ ] T045 [US1] Create `ChatService/Features/Messages/SendMessage/{Command,Endpoint,Handler,Response,Validation}.cs` (envelope validation, dedupe, persist + `LastMessageAt` in one transaction, 201/200 idempotent)
- [ ] T046 [P] [US1] Create `ChatService/Features/Messages/GetHistory/{Endpoint,Handler,Response}.cs` (keyset `before`/`limit` clamp 50–100, `hasMore`, newest first)
- [ ] T047 [P] [US1] Create `ChatService/Features/Conversations/ListConversations/...` and `GetConversation/...` (ordered by `LastMessageAt DESC`, peer summary; `unreadCount` placeholder until US4)
- [ ] T048 [US1] Create `ChatService/Services/INotificationRequestService.cs` + no-op registration (real offline-notification behavior arrives in US5)
- [ ] T049 [US1] Implement `SendMessage` in `ChatService/Hubs/ChatHub.cs` (validation, dedupe, persist, `MessageSentAck`, broadcast `MessageReceived` to `conv:{id}`)
- [ ] T050 [US1] Wire conversation group auto-join on connect in `ChatHub.OnConnectedAsync` via `ChatGroupManager`
- [ ] T051 [US1] Add OpenAPI metadata, error codes (`NOT_PARTICIPANT`, `NOT_CONNECTED`, `INVALID_ENVELOPE`, `PAYLOAD_TOO_LARGE`) and structured logging for send/history paths in `ChatService/Features/Messages/*` and `ChatService/Hubs/ChatHub.cs`

**Review gate (MVP)**: run quickstart V1 and V5 — live delivery, ordered
paged history, restart durability, retry dedupe, connection guard. Stop here
and demo the MVP before starting Phase 4.

---

## Phase 4: User Story 2 — Typing Indicators (Priority: P2)

**Goal**: Live typing indicator that expires automatically and never persists.
**Independent Test**: quickstart V2 — indicator appears within 1 s, clears
within 5 s without another event, and is absent from history responses.

- [ ] T052 [P] [US2] Create `ChatService/Services/TypingTracker.cs` (Redis key `chat:typing:{conversationId}:{userId}`, 5 s TTL, per-connection throttle)
- [ ] T053 [US2] Implement `SetTyping` in `ChatService/Hubs/ChatHub.cs` and broadcast `TypingChanged` to `conv:{id}` excluding the sender
- [ ] T054 [US2] Clear the caller's typing keys in `ChatHub.OnDisconnectedAsync` and when they leave a conversation
- [ ] T055 [P] [US2] Integration tests in `Tests/ChatService.IntegrationTests/TypingTests.cs` (appear ≤1 s, TTL expiry ≤5 s, no persistence)

**Review gate**: quickstart V2 passes; confirm `chatDb` has no typing-related
tables or columns.

---

## Phase 5: User Story 3 — Online Presence (Priority: P3)

**Goal**: Live online/offline status for conversation partners.
**Independent Test**: quickstart V3 — watch a peer, connect/disconnect, observe
`PresenceChanged` and the REST snapshot stay consistent.

- [ ] T056 [P] [US3] Create `ChatService/Services/PresenceTracker.cs` (Redis set `chat:presence:{userId}` of connection ids, 90 s refreshed TTL, transition detection)
- [ ] T057 [US3] Wire presence transitions into `ChatHub.OnConnectedAsync`/`OnDisconnectedAsync` and broadcast `PresenceChanged` to `presence:{userId}` watchers
- [ ] T058 [US3] Implement `WatchPresence` in `ChatService/Hubs/ChatHub.cs` (connected peers only) and `ChatService/Features/Presence/GetPresence/{Endpoint,Handler,Response}.cs`
- [ ] T059 [P] [US3] Integration tests in `Tests/ChatService.IntegrationTests/PresenceTests.cs` (online ≤2 s, offline ≤30 s, reconnect flip, REST snapshot parity)

**Review gate**: quickstart V3 passes; presence truth exists only in Redis
(kill Redis and confirm graceful degradation, not a crash).

---

## Phase 6: User Story 4 — Read Receipts (Priority: P4)

**Goal**: Per-message delivered vs read state plus unread indication.
**Independent Test**: quickstart V4 — recipient marks read, sender sees the
update within 2 s; re-marking is idempotent; unread counts match.

- [ ] T060 [P] [US4] Create `ChatService/Services/ReadReceiptService.cs` (bulk `ReadAtUtc` update where `Id <= upTo`, `SenderId <> reader`, `ReadAtUtc IS NULL`)
- [ ] T061 [US4] Implement `MarkRead` in `ChatService/Hubs/ChatHub.cs` and `ChatService/Features/Messages/MarkRead/{Endpoint,Command,Handler}.cs`, broadcasting `ReadReceiptUpdated` to the sender's `user:{id}` group
- [ ] T062 [US4] Add real `unreadCount` to `ChatService/Features/Conversations/ListConversations/Handler.cs` using the partial unread index (FR-012)
- [ ] T063 [P] [US4] Integration tests in `Tests/ChatService.IntegrationTests/ReadReceiptTests.cs` (receipt ≤2 s, idempotent re-mark, unread count correctness, never-read message stays unread)

**Review gate**: quickstart V4 passes; verify a message never transitions
read → unread.

---

## Phase 7: User Story 5 — Offline Notifications (Priority: P5)

**Goal**: Content-free notification jobs for offline recipients, delivered by
email + push through the background pipeline, without ever blocking messaging.
**Independent Test**: quickstart V6 — offline send creates a content-free
outbox row, the consumer dispatches both channels, redelivery dedupes, and
repeated failure lands on the DLQ while messaging stays healthy.

- [ ] T064 [P] [US5] Create `ChatService/Events/ChatMessageNotificationRequestedEvent.cs` (eventId, messageId, conversationId, senderUserId, recipientUserId, occurredAt, schemaVersion — no content)
- [ ] T065 [US5] Implement `ChatService/Services/NotificationRequestService.cs`: check recipient presence, and if offline write the outbox row in the same transaction as the message (replace the US1 no-op registration)
- [ ] T066 [P] [US5] Create `ChatService/Services/ChatNotificationDispatcher.cs`, `IEmailSender.cs`/`EmailSender.cs`, `IChatPushSender.cs`/`FcmPushSender.cs`, and the logging no-op push sender used when `Chat:Push:Enabled=false`
- [ ] T067 [US5] Create `ChatService/Consumers/ChatNotificationConsumer.cs` (group `chat-notification-worker`, `InboxMessage` dedupe, 3 retries with backoff, publish to `chat-notification-dlq`, commit only on success/final failure)
- [ ] T068 [US5] Finalize `infra/debezium/chat-outbox-connector.json` for `chatDb.public."OutboxMessages"` and align `docker-compose.yml` + `AppHost/Program.cs` connector wiring
- [ ] T069 [P] [US5] Integration tests in `Tests/ChatService.IntegrationTests/NotificationTests.cs` (outbox content-free, dispatch on both enabled channels, redelivery dedupe, DLQ after 3 failures, send path unaffected)
- [ ] T070 [US5] Render notification text from `ChatUserProjection.DisplayName` in `ChatNotificationDispatcher` (sender name only; no content)

**Review gate**: quickstart V6 passes; inspect the outbox payload and confirm
there is no ciphertext, nonce, preview, or plaintext anywhere in the
notification path.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Production-readiness across all stories, executed after US1–US5.

- [ ] T071 [P] Add hub/presence/dispatch metrics and structured logs in `ChatService/Infrastructure/ChatTelemetry.cs`, and Redis/Kafka health checks in `ChatService/Program.cs`
- [ ] T072 [P] Security hardening: verify `Chat:Cors:Origins` is restrictive, add per-connection hub rate limits, document the `X-User-Id` trust boundary in `ChatService/README.md`, and scan for committed secrets
- [ ] T073 [P] Multi-replica validation: add optional `WithReplicas(2)` + distinct `Chat:SnowflakeWorkerId` guidance in `AppHost/Program.cs`/`ChatService/README.md`, run quickstart V8 (cross-instance delivery)
- [ ] T074 [P] Documentation: ChatService section in `README.md`, OpenAPI summary for `/api/chat/**`, and a contract drift check against `specs/001-realtime-chat-service/contracts/`
- [ ] T075 Performance smoke: validate SC-001 (1 s delivery p95), SC-007 (10k-message history), SC-008 (1,000 concurrent sessions) and record results in `specs/001-realtime-chat-service/quickstart.md`
- [ ] T076 Final cleanup: remove temporary scaffolding/echo methods, run `dotnet build "LinkedIn Clone.sln"` + both test suites + quickstart V1–V7, and update the tracking table above

**Review gate**: all quickstart scenarios V1–V8 pass; full test suite green;
constitution Section 3 gates re-verified.

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1** — no dependencies.
- **Phase 2A** — after Phase 1.
- **Phase 2B** — after 2A (needs `ChatDbContext`, projections).
- **Phase 2C** — after Phase 1 (independent of 2A/2B; can run in parallel with 2B).
- **Phase 2D** — after 2A (needs projections) and 2C (resolver).
- **Phase 3 (US1)** — after 2A–2D.
- **Phases 4–7 (US2–US5)** — after Phase 3's hub/group foundation; each is
  independent of the others (US5 needs US3's presence tracker).
- **Phase 8** — after all desired stories.

### User story dependencies

- **US1 (P1)** — depends only on Foundational (MVP).
- **US2 (P2)** — independently testable after US1's hub exists (reuses hub
  plumbing, no dependency on its behavior).
- **US3 (P3)** — independently testable after US1's hub exists.
- **US4 (P4)** — independently testable after US1 (needs messages to mark read).
- **US5 (P5)** — depends on US3 (`PresenceTracker`) for the offline check;
  otherwise independent.

### Within each story

Tests first (fail), then models/services, then endpoints/hub, then validation
and logging. Commit at each review gate.

## Parallel Opportunities

- Phase 1: T003–T010 are independent files → parallel.
- Phase 2A: T011–T019 are separate files → parallel; T020/T021 sequential.
- Phase 2B: T025–T027 consumers are independent → parallel.
- Phase 2C: T029, T031, T032 independent → parallel.
- US1: T039/T040 tests and T042/T043/T046/T047 features are independent →
  parallel; T044/T045/T048–T051 sequential on shared files.
- US2–US4 can be developed by three developers in parallel after US1.
- Polish: T071–T074 independent → parallel.

### Parallel example — US1 features

```text
Task: "Create CreateOrGetConversation feature in ChatService/Features/Conversations/CreateOrGetConversation/"
Task: "Create DeviceKeys features in ChatService/Features/DeviceKeys/"
Task: "Create GetHistory feature in ChatService/Features/Messages/GetHistory/"
Task: "Create ListConversations + GetConversation in ChatService/Features/Conversations/"
```

## Implementation Strategy

### MVP first (Phase 1 → 2 → 3)

Complete Setup + Foundational, then US1. Stop at the Phase 3 review gate,
validate quickstart V1/V5, and demo. This is already a usable messaging
product.

### Incremental delivery (one reviewable phase at a time)

1. Phase 1 → review gate (wiring only).
2. Phase 2A → 2B → 2C → 2D, each with its own gate (data, projections,
   realtime, access).
3. US1 (MVP) → US2 → US3 → US4 → US5, each independently demoable.
4. Phase 8 hardening.

### Parallel team strategy

After Phase 2, one developer owns US1's hub foundation; US2–US4 can proceed in
parallel once it lands. US5 starts after US3. Each story merges behind its own
review gate — no story phase requires another story's features except
US5 → US3.

## Notes

- [P] tasks touch different files and have no pending dependencies.
- Story labels map 1:1 to `spec.md` priorities for traceability.
- Every phase is an independently reviewable diff plus a runnable gate; stop
  and validate at each gate instead of batching multiple stories.
- There is a deliberately small ordering opinion: US5 folds in only after US3
  because offline detection reuses the presence tracker.
