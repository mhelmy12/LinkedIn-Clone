# Phase 0 Research: Real-Time Chat & Messaging Service

**Feature**: `specs/001-realtime-chat-service/spec.md`
**Date**: 2026-11-01
**Status**: All Technical Context unknowns resolved — no open NEEDS CLARIFICATION items.

Each decision is compared against realistic alternatives given the existing
repository (EngagementService PostgreSQL template, Aspire `postgres`/`redis`/
`kafka`, Debezium CDC outbox, IdGen nodes 1–4 used).

---

## R1. Real-time scale-out: SignalR + Redis backplane

**Decision**: Host an ASP.NET Core SignalR hub (`/hubs/chat`) in ChatService and
scale out with `Microsoft.AspNetCore.SignalR.StackExchangeRedis` bound to the
existing Aspire `redis` resource (`AddRedisClient("redis")`).

**Rationale**: FR-007 requires cross-instance delivery. The backplane is the
only option that preserves one logical hub across replicas without sticky
sessions. Redis already exists in the topology and is used by FeedService, so
no new infrastructure class is introduced (Constitution VII). Groups:
`user:{userId}` for per-user events (read receipts, presence) and
`conv:{conversationId}` for message/typing fan-out.

**Alternatives considered**: (a) sticky sessions only — same-node delivery
still breaks after reconnect and doubles memory footprint; (b) Azure SignalR
Service — managed, external, contradicts the self-hosted local topology;
(c) per-instance in-memory with Kafka bridging — Kafka must stay out of the
chat path (FR-015) and adds latency.

---

## R2. Hub authentication through the cookie-stripping gateway

**Decision**: Keep the API Gateway as the authentication boundary. The gateway
validates the Keycloak session (cookie) and its YARP transform injects
`X-User-Id` (the Keycloak `sub`) and `Authorization` on every forwarded request
with an authorization policy, including SignalR negotiate/WebSocket upgrades.
ChatService registers a small `GatewayHeader` authentication scheme that turns
that single header into a `sub` principal, so SignalR can populate
`Context.UserIdentifier` and enforce `RequireAuthorization()` on `/hubs/chat`.
Use the Keycloak `sub` directly as the user identity (same value as
`ChatUserProjection.UserId`), no id translation. No JWT validation, Keycloak
authority configuration or `?access_token=` handling lives in ChatService; REST
endpoints use the same `X-User-Id` convention.

**Rationale**: Every other service already trusts the gateway-injected header,
and production ingress is the gateway only. Validating JWTs again in ChatService
would duplicate Keycloak configuration, put Keycloak metadata availability on
the realtime path, and diverge from the repository's trust model. The header
scheme is still required because SignalR derives `Context.UserIdentifier` from
an authenticated `ClaimsPrincipal`; with no scheme every hub connection would be
anonymous.

**Security boundary**: The header is trusted only because the gateway rewrites
and injects it and strips cookies before forwarding. ChatService's Aspire
external endpoint is a development convenience; exposing the service directly in
production would allow header spoofing, exactly as with the other services. If
native clients ever need to bypass the gateway, add JWT bearer validation back
as a separate hardening step.

**Alternatives considered**: (a) JWT bearer validation in ChatService — rejected
for the duplication and availability coupling above; (b) anonymous hub with a
post-connect auth message — rejected, enables unauthenticated resource
consumption; (c) forward cookies on the chat route only — conflicts with the
global cookie-strip transform and widens session exposure.

---

## R3. End-to-end encryption envelope (client-side)

**Decision**: Client-generated **P-256 ECDH** identity keys per device, HKDF-SHA256
to derive a per-conversation symmetric key, and **AES-256-GCM** for message
payloads. The service stores only: ciphertext, nonce, algorithm version, and
per-device wrapped conversation keys (`ConversationKeyEnvelope`). AAD binds
`conversationId | messageId | senderId`. Private keys never leave the client;
device public keys are registered over REST. Read receipts, typing, presence,
and notification text contain no content.

**Rationale**: FR-021 forbids server-readable content. P-256 + AES-GCM are in
the BCL (`System.Security.Cryptography`), so no new crypto dependency and broad
client-library support. Per-device key envelopes let a user read history on
multiple devices (spec edge case) without exposing plaintext to the server.
No ratchet/forward secrecy in v1 keeps scope sane.

**Alternatives considered**: (a) X25519 — compact and modern, but client-library
support is less uniform than P-256 for the platform's consumers; revisit once
clients adopt .NET 9+/modern WebCrypto everywhere; (b) server-side encryption
only — violates FR-021; (c) Signal-style double ratchet / MLS — strong but
disproportionate complexity for v1, no group chats.

---

## R4. Snowflake IDs with the existing IdGen setup

**Decision**: `builder.Services.AddIdGen(5)` — the next free node after
User=1, Post=2, Engagement=3, Feed=4 — and serialize `long` IDs using the
existing `SnowflakeIdGenerator`/`long.Parse` convention. Conversations and
messages use `long` primary keys with `ValueGeneratedNever()`.

**Rationale**: Matches the user-mandated snowflake requirement, the repo-wide
convention, and keeps IDs time-ordered (used as the message ordering key,
FR-006). Node 5 is verified unused. For multiple replicas, the worker id is
configurable (`Chat:SnowflakeWorkerId`); local development runs one replica.

**Alternatives considered**: (a) PostgreSQL sequences — central coordination,
violates distributed ID requirement; (b) GUIDs — not `long`, breaks the spec;
(c) GUID-to-long hashing — collision risk and non-monotonic.

---

## R5. Connection membership without reading UserDb

**Decision**: Maintain `ChatConnectionProjection` from the existing
`ConnectionAccepted`/`ConnectionRemoved` Kafka events (pairs stored in
normalized min/max order, `IsActive` flag), and on a projection miss or
negative hit perform a one-shot verification against the UserService HTTP API
(`GET /users/connections/{otherUserId}/status`, caller identified by
`X-User-Id`) through Aspire service discovery, caching the answer for 60 s
(positive) / 10 s (negative).

**Rationale**: Satisfies FR-001/FR-016 and Constitution I: no cross-service DB
access. Projection keeps the send hot path local; the API fallback closes the
eventual-consistency window for brand-new connections. UserService already
publishes both events (verified in repo).

**Alternatives considered**: (a) synchronous UserService call per send —
couples the hot path to another service's availability/latency; (b) direct
read of `UserDb` — explicitly forbidden by the spec and Constitution I;
(c) projection only — leaves a seconds-long window where a just-accepted
connection cannot message.

---

## R6. Display name and email for conversation lists and notifications

**Decision**: `ChatUserProjection` (UserId [Keycloak `sub`], DisplayName, Email,
UpdatedAt) fed by `UserProfileUpdated` events, with a UserService API fallback
on miss. User Service events carry the Keycloak `sub` as `UserId`, so no id
translation is required. No `UserProvisioned` event exists in the repo today
(users are provisioned through a Keycloak CDC consumer inside UserService), so
first lookup of a never-updated profile goes through the API and is cached.

**Rationale**: Notifications and conversation headers need a name (FR-013/
FR-014); storing email is required for the email channel. Constitution IV:
plain read model, rebuildable from events + source API.

**Alternatives considered**: (a) join via cross-service query — forbidden;
(b) notification service resolves names — no such service exists in the repo;
(c) omit sender name — violates FR-014.

---

## R7. Offline notification pipeline: outbox + Debezium, not a Kafka producer

**Decision**: When a message is sent to an offline recipient, insert a
`ChatMessageNotificationRequested` v1 outbox row **in the same EF Core
transaction as the message** (EngagementService pattern:
`OutboxMessage{Id Guid, Type, AggregateType, AggregateId, Payload jsonb,
Timestamp}`). Add a Debezium PostgreSQL connector for
`chatDb.public."OutboxMessages"` publishing to a chat topic. ChatService hosts
`ChatNotificationConsumer` (group `chat-notification-worker`) that reads the
Debezium envelope (`After.Payload`), dedupes in `InboxMessage`, and dispatches
email + push. Payload fields: eventId, messageId, conversationId, senderUserId,
recipientUserId, occurredAt — **no content** (FR-021).

**Rationale**: Constitution II requires the transactional outbox; the repo has
no Kafka producer anywhere — Debezium is the established path. Keeping Kafka
off the live path honors FR-015. The README roadmap explicitly anticipates
notification consumers.

**Alternatives considered**: (a) direct `IProducer.Produce` after commit —
dual-write gap; (b) in-process timer/queue — not durable across restarts;
(c) synchronous email call in the request — blocks sends and violates FR-015.

---

## R8. Consumer reliability: inbox dedupe, bounded retry, DLQ

**Decision**: `InboxMessage` (ConsumerGroup, EventId) unique key checked and
written in the same transaction as the effect; manual offset commit only after
success; bounded retries (3, exponential backoff) then publish/log to
`chat-notification-dlq` and commit to unblock the partition.

**Rationale**: At-least-once delivery (Constitution III). Existing consumers
retry forever and never commit on failure, which blocks partitions — the chat
consumer must not copy that defect. Email/push senders are idempotent
externally only by dedupe key, so the inbox is required.

**Alternatives considered**: (a) infinite in-place retry — poison-pill blocks
the partition indefinitely; (b) at-most-once (commit first) — drops
notifications; (c) no dedupe, rely on provider idempotency — email/push
providers generally don't guarantee it.

---

## R9. History retrieval: keyset pagination by Snowflake id

**Decision**: `GET /api/chat/conversations/{id}/messages?before={messageId}&limit=50`
ordered by `Id DESC`, with `limit` clamped (default 50, max 100). The
conversation view renders newest → oldest, "load older" continues from the
last received id.

**Rationale**: FR-022/SC-007. Snowflake ids are time-ordered, so keyset paging
is stable under concurrent inserts and index-friendly
(`(ConversationId, Id DESC)`); 10,000-message conversations stay far below the
2 s open / 1 s page targets.

**Alternatives considered**: (a) OFFSET paging — drifts when new messages
arrive and slows linearly; (b) load-all — violates SC-007 at scale; (c)
timestamp paging — ties and clock skew.

---

## R10. Read receipts: per-message `ReadAtUtc` with bulk mark-up-to

**Decision**: `Message.ReadAtUtc` nullable, set by a single set-based update:
`WHERE ConversationId = @c AND Id <= @upTo AND SenderId <> @reader AND
ReadAtUtc IS NULL`, then broadcast `ReadReceiptUpdated` to the sender's
`user:{senderId}` group.

**Rationale**: In 1-on-1 chats the recipient is implicit, so a nullable column
is sufficient (FR-010/FR-011) and avoids a receipts table. Bounded updates
keep write amplification low on long conversations.

**Alternatives considered**: (a) per-receipt rows — extra table/storage with no
1-on-1 benefit; (b) conversation-level watermark only — cannot distinguish
individual messages as FR-010 requires.

---

## R11. Conversation uniqueness under concurrent creation

**Decision**: Normalize participants as `(min, max)` before persisting; unique
index on `(Participant1Id, Participant2Id)`; on unique-violation (concurrent
first sends) re-read and reuse the existing conversation. `Conversation.Id` and
`LastMessageAt` are maintained in the same transaction as the message insert.

**Rationale**: FR-003/SC-006. Database-enforced uniqueness is the only reliable
guard across replicas; a retry-read converts the race into "return existing".

**Alternatives considered**: (a) advisory locks — Postgres-specific and more
complex; (b) check-then-insert in application code — racy across replicas;
(c) deterministic conversation id derived from the pair — leaks the pair and
complicates Snowflake ordering.

---

## R12. Testing strategy

**Decision**: Keep xUnit 2.9.3 + NSubstitute for unit tests
(`Tests/ChatService.Tests`) following `LinkedIn.Testing.Shared`. Add
`Tests/ChatService.IntegrationTests` with Testcontainers modules for
PostgreSQL, Redis, and Kafka, `WebApplicationFactory<Program>` for REST, and
`Microsoft.AspNetCore.SignalR.Client` for hub flows (two clients, backplane
verification via two app instances against one Redis).

**Rationale**: Constitution Section 3 requires integration coverage for
consumers, projections, and persistence paths; the repo currently has none.
Testcontainers makes those gates CI-runnable without developer-local compose
state. Unit tests stay dependency-free and fast.

**Alternatives considered**: (a) EF InMemory only — cannot prove PostgreSQL
constraints, CDC, or multi-instance SignalR; (b) shared docker-compose infra —
environment-dependent and not isolated; (c) Aspire.Hosting.Testing — heavy for
this repo's current test posture.

---

## R13. Local orchestration and compose parity

**Decision**: In `AppHost/Program.cs`: `var chatDb = postgres.AddDatabase("chatDb");`
then `builder.AddProject<ChatService>("chat-service").WithReference(chatDb, "ChatDbConnection")
.WithReference(redis).WithReference(kafka).WithReference(userService)`; add a
dedicated gateway route for `/hubs/chat` (no prefix rewrite) and a REST route
for `/api/chat/{**catch-all}`. In `docker-compose.yml`, add a PostgreSQL
container (port 5432) plus a ChatService Debezium connector configuration so
the compose path keeps parity with Aspire (Constitution/README invariant).
No fixed ports in the service launch settings; Aspire proxies dynamic ports.
Multi-replica local runs are optional and require distinct
`Chat:SnowflakeWorkerId` values.

**Rationale**: Spec/user constraints and repo conventions. The existing
topology has no PostgreSQL in compose and no replica support; both are
documented gaps this feature closes minimally.

**Alternatives considered**: (a) put `chatDb` on the SQL Server resource —
breaks the user-mandated PostgreSQL requirement; (b) skip compose parity —
violates the README's stated Aspire/compose alignment; (c) fixed ports for
replicas — collides on a developer machine.

---

## R14. Push channel without existing provider infrastructure

**Decision**: Define `IChatPushSender` with provider configuration
(`Chat:Push:Provider`, credentials via configuration/secret store). Ship an
FCM HTTP v1 adapter and a logging no-op used when push is disabled locally;
the notification dispatcher sends email (real provider config required) and
push on every enabled channel, recording per-channel outcomes.

**Rationale**: FR-014 mandates both channels; the repository has no push
infrastructure today. Keeping the provider behind an interface and a no-op
local default lets the pipeline and tests run without credentials while
production wiring stays a configuration task.

**Alternatives considered**: (a) deliver push through SignalR — impossible for
offline users; (b) skip push for v1 — contradicts the clarify decision (C);
(c) build a new NotificationService — out of scope and violates YAGNI.
