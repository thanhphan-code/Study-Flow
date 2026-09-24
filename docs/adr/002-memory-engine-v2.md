# ADR 002: Memory Engine V2

- Status: Accepted
- Date: 2026-09-20

## Context

The unified learning pipeline in ADR 001 removed mode-specific progress writes, but its first scheduler still treated most successful answers alike. Recognition, unaided recall, hinted answers, response time and repeated failure carry different evidence about memory. The system also needed reproducible decisions, user-local calendar boundaries and stronger protection against concurrent retries.

## Decision

StudyFlow evolves the central pipeline into Memory Engine V2 while keeping the existing HTTP routes compatible.

### Evidence model

Every `LearningAttempt` remains immutable evidence. `FlashcardProgress` is the current projection and additionally tracks:

- consecutive correct and wrong answers;
- recent wrong answers;
- successful recall versus recognition;
- hinted versus unhinted success;
- response-time aggregates;
- scheduler version, last scheduling time and state revision.

An unaided recall or application answer contributes more evidence than recognition. A hinted, slow or low-confidence answer receives a smaller scheduling benefit. Repeated wrong answers increase weakness and shorten the next interval.

### Versioned policy and scheduler

`ISrsScheduler` owns interval, ease and next-review calculations. `ILearningStatePolicy` owns mastery, weakness, priority and the recommended next learning step. Their current versions are `simple-srs/2` and `memory-state/2`.

Intervals are capped at 3650 days. This is a product constraint and also prevents date overflow after long success streaks.

### Decision audit

Each committed attempt creates one `LearningDecision` snapshot containing before/after mastery, weakness, interval, next-review time and action, together with policy/scheduler versions and a JSON decision trace. The unique foreign key to `LearningAttempt` enforces one decision per committed attempt.

### Consistency and idempotency

PostgreSQL transactions acquire a per-user/per-card advisory lock. `(UserId, ClientAttemptId)` is unique. Reusing the same key and payload returns the existing result; reusing it with different evidence returns `IDEMPOTENCY_KEY_REUSED`.

The development connection pool is capped below PostgreSQL's connection limit so burst traffic does not block administrative or health-check connections.

### User-local calendar

Users have an IANA/OS-compatible `TimeZoneId`. Registration captures the browser time zone, and `PUT /api/auth/timezone` allows it to be changed. Dashboard daily totals, history buckets and streaks use UTC instants converted through the user's calendar, including daylight-saving transitions.

## Verification

- Unit and API integration tests cover evidence weighting, interval limits, time-zone rollover/DST, decision versions and idempotency conflicts.
- A real PostgreSQL burst test with 100 unique attempts on one card completed without lost updates.
- A real PostgreSQL burst test with 100 identical requests returned 100 successful responses while persisting exactly one attempt, one decision and one state revision.

## Consequences

Positive:

- All learning modes now influence the same richer memory model.
- Scheduling and recommendation decisions are explainable and replayable by version.
- User-facing day and streak calculations match the learner's local calendar.
- Duplicate requests and concurrent tabs cannot inflate progress.

Trade-offs:

- `FlashcardProgress` is deliberately denormalized and must only be changed through the engine.
- The formulas are heuristic, not a trained memory model; versioned decision data enables later calibration.
- Existing users default to UTC until they explicitly update their time zone.
- Historical aggregate progress can only be backfilled approximately because old raw evidence did not contain every V2 signal.

## Deferred work

Document processing, source chunking and retrieval quality are a separate pipeline and are not coupled to the memory-state transaction. They should be implemented as a later bounded epic with asynchronous jobs and object storage.
