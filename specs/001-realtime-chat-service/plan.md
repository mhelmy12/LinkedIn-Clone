# Implementation Plan: Real-Time Chat & Messaging Service

**Branch**: `001-realtime-chat-service` | **Date**: 2026-11-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-realtime-chat-service/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command; its definition describes the execution workflow.

## Summary

Add a new **ChatService** microservice to the existing .NET 9 / .NET Aspire
solution, implementing 1-on-1 conversations between accepted connections:
real-time message exchange, typing indicators, online presence, per-message
read receipts, paged history, and offline email/push notifications.

Technical approach (all decisions grounded in `research.md`):

- **Transport**: ASP.NET Core SignalR hub at `/hubs/chat`, scaled out with the
  `Microsoft.AspNetCore.SignalR.StackExchangeRedis` backplane over the
  existing Aspire `redis` resource, so users connected to different replicas
  still exchange messages (FR-007).
- **Data**: `ChatService` owns PostgreSQL database `chatDb` (new database on
  the existing Aspire `postgres` server), with EF Core 9 + Npgsql following
  the EngagementService conventions. No access to `UserDb`/`PostDb` (FR-016).
- **IDs**: Snowflake `long` IDs through the existing IdGen setup, using the
  free node **5** (User=1, Post=2, Engagement=3, Feed=4).
- **Privacy**: message contents are end-to-end encrypted at the client
  (P-256 ECDH envelope + AES-256-GCM); the service stores ciphertext, nonce,
  and wrapped conversation keys only (FR-021). Typing, presence, read state,
  and notification payloads are content-free metadata.
- **Asynchrony**: offline notifications are requested via a transactional
  outbox row (same transaction as the message) and transported by Debezium CDC
  to Kafka; Kafka is never on the chat delivery path (FR-014/FR-015).
  A hosted consumer (inbox-deduplicated) dispatches email and push.
- **Cross-service facts**: connection membership and display names arrive as
  Kafka projections (`ConnectionAccepted`, `ConnectionRemoved`,
  `UserProfileUpdated`) plus an on-demand UserService API fallback, satisfying
  FR-001 without cross-database reads. User identity is the Keycloak `sub`
  string everywhere (the repo-wide convention), so projections key on it
  directly; only Conversation/Message use Snowflake `long` ids.

The main complexity accepted is end-to-end encryption (no server-readable
content) and multi-replica real-time state; both are mandated by the spec and
are contained behind explicit seams (crypto envelope stored as opaque bytes,
SignalR backplane, notification dispatcher interface).

## Technical Context

**Language/Version**: C# 13 on .NET 9 (`net9.0`), ASP.NET Core 9

**Primary Dependencies**: ASP.NET Core SignalR + `Microsoft.AspNetCore.SignalR.StackExchangeRedis` (9.x backplane); ASP.NET Core authentication (gateway-header scheme, no JWT package); EF Core 9.0.19 + Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4; Confluent.Kafka 2.15.0; IdGen 3.0.7 (node 5); Carter 8.2.1 + MediatR 12.5.0 + FluentValidation 12.1.0 via `MyBank.Shared.Core` 1.0.4; Aspire.StackExchange.Redis 13.5.3; `ServiceDefaults` project reference

**Storage**: PostgreSQL `chatDb` (conversations, messages, projections, outbox, inbox, device keys); Redis (SignalR backplane, presence, typing); Kafka (outbox → notification jobs only). No other service database is touched.

**Testing**: xUnit 2.9.3 + NSubstitute + EF InMemory (existing `Tests/` conventions); new integration layer adds Testcontainers (PostgreSQL/Redis/Kafka), `WebApplicationFactory<Program>`, and `Microsoft.AspNetCore.SignalR.Client`

**Target Platform**: Linux containers; Windows/macOS developer workstations; local orchestration through .NET Aspire AppHost (Docker Desktop)

**Project Type**: web-service (new microservice) inside the existing distributed solution

**Performance Goals**: 95% of messages visible to an online recipient within 1 s (SC-001); typing indicator ≤1 s on / ≤5 s off (SC-002); presence online ≤2 s, offline ≤30 s (SC-003); read receipts ≤2 s (SC-004); conversation opens ≤2 s at 10,000 messages, older page ≤1 s (SC-007); 1,000 concurrent sessions per instance without missing the 1 s target (SC-008); offline notification ≤2 min (SC-009)

**Constraints**: content E2E-encrypted, server holds no decryptable content (FR-021); at most one conversation per normalized user pair; Kafka background-only; Snowflake `long` IDs; the gateway authenticates and injects `X-User-Id` (Keycloak `sub`) on forwarded requests including hub upgrades, and strips cookies — ChatService maps that header to a SignalR principal and adds a configurable CORS policy (no JWT validation or `access_token` query handling); no sticky sessions needed thanks to the backplane; local single-replica by default (multi-replica Snowflake worker IDs via configuration)

**Scale/Scope**: 1-on-1 only (no groups/media); up to 10,000 messages per conversation with keyset paging; 1,000 concurrent live sessions per instance; background notification processing at-least-once

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status | Evidence |
|-----------|------|--------|----------|
| I. Service Ownership and Explicit Boundaries | ChatService owns `chatDb`; zero cross-service DB access | PASS | FR-016; connection/profile facts arrive via Kafka projections + UserService API fallback (research R5/R6) |
| II. Event-Driven Propagation via Transactional Outbox | Notification request written in the same transaction as the message | PASS | Outbox row per offline message; Debezium CDC → Kafka (research R7); no Kafka producer on the send path |
| III. Idempotent Consumers Under At-Least-Once Delivery | Notification consumer deduplicated; send path de-duplicated | PASS | `InboxMessage` keyed by event ID; unique `(ConversationId, SenderId, ClientMessageId)`; retry-then-DLQ (research R8) |
| IV. Read Models Over Cross-Service Joins | Hot state lives in Redis; projections are rebuildable | PASS | Presence/typing in Redis with TTL; `ChatUserProjection`/`ChatConnectionProjection` rebuilt from events (research R5/R6) |
| V. Event Contracts and Versioning | New event is additive and versioned | PASS | `ChatMessageNotificationRequested` v1, additive fields only; consumed events copied per service per existing convention (research R7) |
| VI. Security, Identity, and Secrets Hygiene | AuthN via Keycloak/gateway; no secrets committed; content private | PASS | Gateway-injected `X-User-Id` mapped to a SignalR principal through a header scheme; trust boundary documented; E2E ciphertext-only storage; no credentials in repo |
| VII. Observability and Measured Evolution | ServiceDefaults everywhere; no unjustified infrastructure | PASS | `AddServiceDefaults()` + `MapDefaultEndpoints()`; hub/presence metrics; reuse existing postgres/redis/kafka resources (no new infra class) |

Section 3 quality gates: no new cross-service DB access; additive event
contract; declared idempotency for the consumer; outbox transactional with
business data; no secrets; health/logs/traces intact. **No violations; no
Complexity Tracking entries required.**

**Post-Phase 1 re-evaluation: PASS.** `data-model.md` and the three contracts
keep every gate satisfied: projections replace cross-service queries, the
notification event is written in the message transaction, the consumer is
inbox-deduplicated, event/hub payloads are additive-only, storage holds
ciphertext only, and observability stays on ServiceDefaults. No new
violations introduced by the design.

## Project Structure

### Documentation (this feature)

```text
specs/001-realtime-chat-service/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── rest-api.md
│   ├── signalr-hub.md
│   └── kafka-events.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ChatService/
├── ChatService.csproj
├── Program.cs                       # ServiceDefaults, EF/ChatDbContext, SignalR+Redis backplane,
│                                    # gateway-header auth, Carter, hosted consumers
├── appsettings.json
├── appsettings.Development.json
├── Properties/launchSettings.json
├── Data/
│   ├── ChatDbContext.cs
│   ├── ConversationConfiguration.cs
│   ├── MessageConfiguration.cs
│   ├── ConversationKeyEnvelopeConfiguration.cs
│   ├── UserDeviceKeyConfiguration.cs
│   ├── ChatUserProjectionConfiguration.cs
│   ├── ChatConnectionProjectionConfiguration.cs
│   ├── OutboxMessageConfiguration.cs
│   └── InboxMessageConfiguration.cs
├── Models/
│   ├── Conversation.cs
│   ├── Message.cs
│   ├── ConversationKeyEnvelope.cs
│   ├── UserDeviceKey.cs
│   ├── ChatUserProjection.cs
│   ├── ChatConnectionProjection.cs
│   ├── OutboxMessage.cs
│   └── InboxMessage.cs
├── Features/                        # Carter + MediatR + FluentValidation per use case
│   ├── Conversations/
│   │   ├── CreateOrGetConversation/{Command,Endpoint,Handler,Response,Validation}.cs
│   │   ├── ListConversations/{...}.cs
│   │   └── GetConversation/{...}.cs
│   ├── Messages/
│   │   ├── GetHistory/{...}.cs
│   │   ├── SendMessage/{...}.cs
│   │   └── MarkRead/{...}.cs
│   ├── DeviceKeys/
│   │   ├── RegisterDeviceKey/{...}.cs
│   │   └── GetPeerKeys/{...}.cs
│   └── Presence/
│       └── GetPresence/{...}.cs
├── Hubs/
│   ├── ChatHub.cs                   # SendMessage, MarkRead, SetTyping, presence lifecycle
│   └── ChatHubEvents.cs             # server→client event name constants
├── Consumers/
│   ├── ConnectionAcceptedConsumer.cs
│   ├── ConnectionRemovedConsumer.cs
│   ├── UserProfileUpdatedConsumer.cs
│   └── ChatNotificationConsumer.cs  # inbox-deduped email/push dispatch
├── Services/
│   ├── ConversationAccessService.cs # pair normalization, connect check, idempotent create
│   ├── PresenceTracker.cs           # Redis connection counts + TTL
│   ├── TypingTracker.cs             # ephemeral Redis typing keys
│   ├── ReadReceiptService.cs        # bulk mark-read up to message id
│   ├── NotificationRequestService.cs# offline check + outbox write (same tx)
│   ├── ChatNotificationDispatcher.cs
│   ├── IEmailSender.cs / EmailSender.cs
│   └── IPushSender.cs / PushSender.cs
├── Helpers/KafkaConfiguration/KafkaOptions.cs
└── Migrations/                      # EF Core migrations owned by ChatService

Tests/
├── ChatService.Tests/               # unit: pair normalization, Snowflake ordering,
│                                    # envelope validation, read-up-to, projections
└── ChatService.IntegrationTests/    # Testcontainers: PostgreSQL, Redis, Kafka;
                                     # WebApplicationFactory REST + SignalR client hub flows
```

**Structure Decision**: A new root-level service project, mirroring
`EngagementService`'s feature-oriented layout (`Data/`, `Models/`, `Events/`,
`Features/<Aggregate>/<UseCase>/`, `Consumers/`, `Migrations/`) and the
repository's flat root-project + `Tests/` solution-folder convention. Touch
points outside `ChatService/` are limited to: `AppHost/Program.cs` (register
`postgres.AddDatabase("chatDb")` + `chat-service` with references to
`chatDb`/`redis`/`kafka`), `APIGateway/appsettings.json` (REST + hub routes),
`docker-compose.yml` (PostgreSQL service for compose parity), the Debezium
connector for the chat outbox, and `LinkedIn Clone.sln`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations. Deliberately accepted complexity (mandated by the
spec, not governance exceptions): client-side E2E envelope encryption, Redis
backplane, and a notification consumer with inbox deduplication + DLQ.
