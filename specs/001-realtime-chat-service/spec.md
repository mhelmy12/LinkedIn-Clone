# Feature Specification: Real-Time Chat & Messaging Service

**Feature Branch**: `001-realtime-chat-service`

**Created**: 2026-11-01

**Status**: Draft

**Input**: User description: "Implement a 1-on-1 real-time chat system for the
LinkedIn Clone. Users must be able to message their connections instantly, see
typing indicators, know when the other user is online, and receive read
receipts. Architectural constraints (STRICT, user-mandated): create a new
ChatService microservice that owns its own PostgreSQL database (chatDb) and
must not read/write UserDb or PostDb; use SignalR for WebSockets; use Redis
Pub/Sub to bridge messages between multiple service instances; use Kafka ONLY
for background tasks (e.g., email or push notification to an offline user) and
never for actual chat messages; use Snowflake IDs (long) for Conversations and
Messages." (Full text preserved in conversation; constraints retained verbatim
under "User-Mandated Architectural Constraints".)

## Clarifications

### Session 2026-11-01

- Q: How confidential must chat messages be — who is allowed to read their contents? → A: B — end-to-end encrypted; only the two participants can ever read the contents (no server-readable previews).
- Q: When a user opens a conversation, how much history should load at once? → A: B — the most recent ~50 messages load first; older pages load on demand without limit.
- Q: Which delivery channel should offline notifications use for v1? → A: C — both email and push, delivered on every notification channel the user has enabled.
- Q: What should the sender see when a message fails to send? → A: A — a distinct visible "failed" state with an explicit retry action (dedupe-safe).
- Q: What should happen to a user's conversations and messages when they delete or close their account? → A: B — conversations are kept; the closed account is anonymized in the counterpart's view; new messages to/from the closed account are blocked.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Exchange Messages with a Connection (Priority: P1)

A signed-in user selects one of their accepted connections and sends a text
message. The message appears in the recipient's open conversation instantly,
without any refresh or manual reload. Every message is durably stored, so when
either participant returns later, the full conversation history is retrievable
in the exact order the messages were sent. A conversation is created
automatically on the first message between a pair, and thereafter the same two
users always share exactly one conversation — no duplicates, ever.

**Why this priority**: This is the core value of the feature; every other
capability (typing, presence, receipts, notifications) is a real-time signal
layered on top of message exchange. With only this story implemented, users
already have a usable messaging product.

**Independent Test**: Connect two accounts, send messages from one to the
other, and directly verify instant arrival plus full ordered history retrieval
after reconnecting. No other story needs to exist for this to work.

**Acceptance Scenarios**:

1. **Given** Users A and B are accepted connections and B is online with the
   conversation open, **When** A sends a message, **Then** B sees the message
   without refreshing, and A receives confirmation that the message was sent.
2. **Given** no conversation exists between A and B, **When** A sends the
   first message, **Then** a conversation is created automatically and the
   message is attached to it.
3. **Given** a conversation between A and B already exists, **When** either
   user attempts to start a conversation again (including at the same time
   from both sides), **Then** the existing conversation is reused and exactly
   one conversation exists for the pair.
4. **Given** a conversation with prior messages, **When** a user opens it at
   any later time, **Then** the most recent messages appear first and any
   older pages the user requests appear in the same single order the messages
   were sent.
5. **Given** User A is NOT a connection of User B, **When** A attempts to
   message B, **Then** the attempt is rejected with clear explanatory
   feedback and no conversation or message is created.

---

### User Story 2 - See the Other Person Typing (Priority: P2)

While a user is typing in an open conversation, the counterpart — who is also
viewing that conversation — sees a live typing indicator. The indicator
disappears when the user stops typing or leaves the conversation. It is
ephemeral: no typing state is ever stored or shown later.

**Why this priority**: Typing feedback makes the conversation feel live and
prevents double-sending, but it is additive — messaging works fully without
it, which is why it is not P1.

**Independent Test**: Open the same conversation on two accounts; start
typing on one side and observe the indicator appear on the other; stop typing
and observe it disappear. Requires only Story 1 infrastructure to test.

**Acceptance Scenarios**:

1. **Given** A and B both have the conversation open, **When** A starts
   typing, **Then** B sees an active typing indicator within moments of the
   keystrokes, without refreshing.
2. **Given** B is viewing a typing indicator, **When** A stops typing for a
   few seconds, **Then** the indicator disappears.
3. **Given** B is viewing a typing indicator, **When** A closes the
   conversation or goes offline, **Then** the indicator disappears promptly.

---

### User Story 3 - Know When the Other Person Is Online (Priority: P3)

In the chat context, a user can see whether their connection is currently
online or offline. Presence updates live: when the counterpart connects or
disconnects, the status flips without a refresh. Presence is a live signal,
not a stored attribute.

**Why this priority**: Presence sets reply-time expectations and complements
typing indicators, but the messaging experience stands alone without it.

**Independent Test**: Sign a second user in and out while the first user
views the chat, and verify the status change is reflected live each time.

**Acceptance Scenarios**:

1. **Given** B is currently connected, **When** A views the conversation with
   B, **Then** B is shown as online.
2. **Given** B is shown as online, **When** B disconnects or closes their
   session, **Then** A's view updates to offline without a refresh.
3. **Given** B is shown as offline, **When** B reconnects, **Then** A's view
   updates to online without a refresh.

---

### User Story 4 - Get Read Receipts (Priority: P4)

A sender can see, per message, whether the recipient has read it. A message
shows a distinct state while delivered-but-unread, and transitions to a read
state once the recipient actually views it. Messages are marked as read only
when the recipient genuinely has the message visible.

**Why this priority**: Read receipts close the communication loop and are a
strong expectation in modern messaging, but they depend on messages (P1) and
benefit from presence (P3) preceding them.

**Independent Test**: Send a message from A to B with the conversation closed
on B's side (state: unread); open the conversation on B's side and observe
A's view flip that message to read.

**Acceptance Scenarios**:

1. **Given** A sent a message that B has not viewed, **When** A views the
   conversation, **Then** the message shows an unread/delivered state distinct
   from any read state.
2. **Given** B opens the conversation containing A's unread message such that
   it is visibly displayed, **When** the read event is processed, **Then** A's
   view of that message changes to read within a couple of seconds, without
   refreshing.
3. **Given** B never opens the conversation, **When** A views the
   conversation, **Then** that message never shows a read state.

---

### User Story 5 - Notify Offline Users of Missed Messages (Priority: P5)

When a message is sent to a user who is not currently online, the platform
initiates a background follow-up task that delivers a notification through
the platform's existing channels, on every channel the recipient has enabled
(email and push). This happens through the platform's asynchronous background
pipeline — it is deliberately not part of the real-time message delivery
path, and failures there never block message sending.

**Why this priority**: It re-engages users who are away but is entirely
independent: chat works in real time for online users regardless of whether
this story ships.

**Independent Test**: With B's session fully disconnected, send a message
from A and verify B later receives a notification containing the sender's
name only — contents stay private per FR-021 — through each enabled channel.

**Acceptance Scenarios**:

1. **Given** B is offline, **When** A sends B a message, **Then** B receives
   a notification naming the sender and prompting them to open the platform,
   without exposing message contents.
2. **Given** B is online, **When** A sends B a message, **Then** no offline
   notification for that message is triggered.
3. **Given** messaging overall is unaffected, **When** the notification step
   fails or is delayed, **Then** the message itself was already delivered and
   stored normally — senders are never blocked by notification problems.

---

### Edge Cases

- What happens when both users send the first message of a brand-new
  conversation at effectively the same time? Exactly one conversation is
  created; both messages end up in that single conversation with stable order.
- What happens when a user taps "send" twice in quick succession for the same
  content with the first send still in flight? Only one message is created
  (the duplicate tap does not produce a visible duplicate).
- What happens when the recipient's connection drops while a message is in
  transit? The message remains persisted; the recipient gets it on reconnect
  (via history or delivered live), and the sender sees it as unread rather
  than lost.
- What happens when the send operation itself fails (network loss, service
  restart, rejection by the server)? The message enters a distinct "failed"
  state on the sender's side and can be explicitly retried without ever
  creating a duplicate message.
- What happens when a user tries to send a message while the counterpart
  removes their connection at that moment? Subsequent sends between the pair
  are rejected with clear feedback; history remains viewable.
- What happens when one participant deletes or closes their account? Their
  remaining conversations stay retrievable for the counterpart but with the
  closed account anonymized; new messages to or from the closed account are
  rejected.
- What happens when a message is very long or contains unusual characters,
  emojis, line breaks, or links? It is rejected only at the defined length
  limit with clear feedback; everything under the limit is accepted and
  rendered as sent.
- What happens when a user opens multiple devices/tabs at once? Messages,
  read receipts, and presence stay consistent; read state is global to the
  user, and being active on any device counts as online (per-device presence
  precision is out of scope).
- How does the system order messages sent nearly simultaneously by both
  sides? All messages in a conversation are presented in a single agreed
  order to both participants.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow message exchange only between users who are
  accepted connections of each other.
- **FR-002**: System MUST reject an attempted message or conversation from a
  user to a non-connected user with clear, explanatory feedback and without
  persisting any data for the attempt.
- **FR-003**: System MUST guarantee at most one conversation between a given
  pair of users, including when both users initiate conversation at the same
  time.
- **FR-004**: System MUST durably persist every accepted message such that it
  survives a service restart and remains retrievable in conversation history.
- **FR-005**: System MUST deliver messages to an online recipient viewing the
  conversation in real time, without requiring any refresh or manual reload.
- **FR-006**: System MUST present each conversation's messages to both
  participants in one consistent order, matching the order the messages were
  accepted.
- **FR-007**: System MUST support live message exchange between two users
  even when their sessions are served by different service instances.
- **FR-008**: System MUST show a typing indicator to the counterpart while
  the user is actively typing and MUST clear it promptly after the user stops
  typing, leaves the conversation, or disconnects. Typing state MUST be
  ephemeral (never stored or replayed later).
- **FR-009**: System MUST expose each participant's online/offline presence
  in the chat context and MUST update it live when the participant connects,
  disconnects, or reconnects. Presence MUST be computed from current activity,
  not from stored profile data.
- **FR-010**: System MUST allow a sender to distinguish, per message,
  delivered-but-unread from read state.
- **FR-011**: System MUST transition a message to read state when, and only
  when, the recipient has the message actually displayed (i.e., conversation
  open and the message visible), regardless of which device they use.
- **FR-012**: System MUST show an unread indication (badge/count or new
  marker) on conversations that contain messages the user has not yet seen.
- **FR-013**: System MUST present each user a list of their conversations
  ordered by most recent activity in that conversation.
- **FR-014**: For each message sent to an offline recipient, System MUST
  trigger a follow-up notification through the platform's background pipeline
  on every notification channel the recipient has enabled — for v1, both
  email and push — naming the sender. Because message contents are
  end-to-end encrypted per FR-021, notifications MUST NOT include any message
  content.
- **FR-015**: System MUST NOT use the background pipeline for real-time
  message delivery; real-time delivery MUST go through the dedicated live
  messaging channel, and background-pipeline failures MUST NOT block or
  prevent message sending or delivery.
- **FR-016**: ChatService (the new service implementing this feature) MUST
  own exactly one dedicated chat data store and MUST NOT read or write any
  other service's database; all other data it needs (e.g., connection facts,
  identity) MUST reach it through published APIs or event streams, never
  through direct cross-service database access.
- **FR-017**: System MUST reuse the platform's existing authentication so
  users only ever see their own conversations and messages.
- **FR-018**: System MUST enforce a maximum message length and reject longer
  submissions with clear feedback while accepting the full allowed content
  (text, emojis, line breaks, links).
- **FR-019**: System MUST tolerantly handle duplicate send taps/simultaneous
  identical submissions so they materialize as a single message, and MUST
  keep conversation creation idempotent per user pair.
- **FR-020**: System MUST apply consistent per-instance limits to live
  messaging connections so a single misbehaving client cannot starve other
  users (rate/abuse limits; exact thresholds deferred to planning).
- **FR-021**: System MUST encrypt message contents end-to-end so that only
  the two conversation participants can decrypt and read them; the chat data
  store and the service itself MUST NOT hold message contents in decryptable
  form, and every feature that touches message text (notifications, previews,
  future search) MUST work from content-free metadata only.
- **FR-022**: When a user opens a conversation, System MUST load the most
  recent ~50 messages first, and MUST provide on-demand loading of older
  history in bounded pages with no overall limit, always presented within the
  conversation's single consistent order (FR-006).
- **FR-023**: System MUST surface a distinct, visible "failed" state on the
  sender's copy of any message that was not durably persisted, and MUST
  provide an explicit retry action for it; a retry MUST reuse the duplicate
  safeguard of FR-019 so it can never materialize as a second message.
- **FR-024**: When a user's platform account is deleted or closed, System
  MUST anonymize that user within the counterpart's view of their shared
  conversations (message contents and metadata retained), MUST block new
  messages to or from the closed account, and MUST stop exposing the closed
  user's profile data through chat surfaces.

### User-Mandated Architectural Constraints (STRICT, retained verbatim)

The following constraints were mandated by the product owner in the feature
request and are binding requirements on this feature. They are retained
because they were explicitly specified as strict, not because the spec
process derives from them. Where a constraint uses named technologies,
mission-level intent is noted so planning may honor both.

1. A completely new `ChatService` microservice MUST be created.
2. `ChatService` owns its own PostgreSQL database (`chatDb`) and MUST NOT
   read/write `UserDb` or `PostDb`. (Constitution Principle I: service data
   ownership — compliant.)
3. Real-time transport MUST be SignalR (WebSockets).
4. Because SignalR keeps state in memory, Redis Pub/Sub MUST broadcast
   messages between multiple `ChatService` instances; a user on Node 1 talks
   with a user on Node 2 across the bridge. (Supports FR-007.)
5. Kafka is used ONLY for background tasks (e.g., email/push notification to
   an offline user); actual chat messages MUST NOT flow through Kafka.
   (Supports FR-014/FR-015.)
6. IDs for Conversations and Messages MUST be Snowflake IDs (`long`).
   (Mission intent: time-ordered, globally unique numeric identifiers without
   central coordination.)

Constitution alignment notes: any Kafka consumer built for this feature MUST
be idempotent (Constitution Principle III); chatDb schema and migrations
belong to ChatService; observability (health checks, structured logs, traces)
MUST use the shared ServiceDefaults (Constitution Principle VII).

### Key Entities *(include if feature involves data)*

- **Conversation**: A persistent 1-on-1 chat between exactly two users.
  Identified by a unique, time-ordered numeric identifier; records which two
  users participate (with their relative ordering fixed so the pair is
  always represented identically), when the conversation was created, and
  when the most recent message occurred. Uniquely identified by the
  participant pair — never duplicated.
- **Message**: A single text entry inside a conversation. Identified by a
  unique, time-ordered numeric identifier (orderable, serving as the
  conversation's ordering key); records the conversation, the sender, the
  content, the sent time, and the read state as seen by the recipient.
- **Presence & typing state**: Ephemeral live signals, explicitly NOT
  persisted entities; derived from currently active sessions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For online users with the conversation open, 95% of sent
  messages are visible to the recipient within 1 second of sending; no
  refresh required.
- **SC-002**: A typing indicator appears within 1 second of the counterpart
  starting to type and disappears within 5 seconds of them stopping.
- **SC-003**: Presence status reflects online within 2 seconds of a
  reconnection and offline within 30 seconds of a disconnect, without a
  refresh.
- **SC-004**: Read receipts flip to read within 2 seconds for the sender once
  the recipient has actually viewed the message.
- **SC-005**: 100% of accepted messages are retrievable in correct order from
  history after service restarts — including messages sent seconds before a
  restart.
- **SC-006**: Zero duplicate conversations can ever exist for the same user
  pair, including simultaneous first sends from both sides (verified by
  concurrent-creation tests).
- **SC-007**: Users can open any conversation and see its latest history
  within 2 seconds at up to 10,000 messages in that conversation, and any
  requested older page loads within 1 second.
- **SC-008**: The system sustains at least 1,000 concurrent active chat
  sessions per instance without degrading the 1-second delivery target of
  SC-001, and cross-instance conversations (different users on different
  instances) meet the same target.
- **SC-009**: Offline recipients receive their notification within 2 minutes
  of the message being sent (platform background pipeline target).
- **SC-010**: 100% of message attempts to non-connected users are rejected
  with explanatory feedback, and create no data.
- **SC-011**: When a user's account is closed or deleted, every shared
  conversation of that user reflects the anonymized identity and blocked
  sending within 1 minute of the closure, verified across 100% of closure
  scenarios.

## Assumptions

- **Scope**: 1-on-1 chats only for v1; group chats, media/photo attachments,
  voice messages, message reactions, editing, deleting, forwarding, drafts,
  and scheduled messages are explicitly OUT of scope for this feature.
- **Connections**: "Connections" means accepted connections as managed by the
  existing platform identity/connection domain. Only accepted connections can
  exchange messages. How ChatService stays current on connection facts
  (synchronous validation vs. event-projected read model) is a planning
  decision; it MUST NOT read other service databases directly (FR-016).
- **Connection removal**: When a connection is removed between two users,
  existing conversation history remains viewable, but new messages between
  the pair are rejected per FR-002. (Example of a reasonable default chosen
  by the spec-writer.)
- **Content limits**: Plain text; maximum message length set at 2,000
  characters (industry-standard default); rendering matches sent content,
  including links, emojis, and line breaks.
- **Read marking**: A message counts as read when it is visibly displayed on
  any of the recipient's active devices; per-device read tracking is out of
  scope.
- **Presence definition**: A user is "online" while holding at least one
  active authenticated live session; presence has no persistence and no
  grace period beyond what SC-003 allows.
- **Offline notifications**: Use the platform's existing notification
  infrastructure, delivering on both enabled channels (email and push) per
  the clarify decision; one notification is triggered per message sent while
  the recipient is offline, on each enabled channel; notification quiet times
  and digests are out of scope.
- **Data retention**: Conversations and messages are retained indefinitely
  for active accounts (standard default for this product family); accounts
  that are deleted or closed follow FR-024 instead (anonymized identity,
  blocked exchange, retained conversation contents). Retention/regulation
  policies beyond that are a future constitutional/compliance concern and
  out of scope here.
- **Authentication**: Users are already authenticated via the existing
  platform identity provider (OIDC sessions); chat adds no new sign-in flow.
- **Dependencies**: Requires availability of the user/connection domain data
  (API or events, per the architecture), the existing notification
  pipeline, and clients with real-time connectivity.
- **Clarifications**: No [NEEDS CLARIFICATION] markers are present in this
  spec; all gaps above were resolved with documented, reasonable defaults.
