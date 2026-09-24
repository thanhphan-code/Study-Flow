# ADR 001: Unified Learning Engine

- Status: Accepted
- Date: 2026-09-20

## Context

StudyFlow supports Flashcard Study, Due Review, Quiz, Smart Learn, Daily Study and Study Together. Previously, each mode owned part of the decision process for selecting cards, recording answers, updating spaced repetition and detecting weak cards. This caused different modes to produce inconsistent learner state. Quiz results, for example, were not reflected in flashcard progress.

## Decision

All learning writes pass through `ILearningEngine`.

The engine:

1. Validates card ownership and session membership.
2. records an immutable `LearningAttempt` with its learning mode and evidence;
3. prevents duplicate client attempts;
4. updates `FlashcardProgress` through the shared spaced-repetition scheduler;
5. recalculates mastery and weakness signals through `ILearningStatePolicy`;
6. updates study-session counters when appropriate; and
7. commits the changes atomically, with a per-user/per-card PostgreSQL advisory lock.

`LearningAttempt` is the historical evidence log. `FlashcardProgress` is the current projection used for fast reads. The state policy also computes the recommended next action at query time because due state changes with time.

## Mode integration

- Flashcard Study and Due Review call the review endpoint, which records evidence through the engine.
- Daily Study uses a `DailyStudy` session and the same engine-backed review endpoint.
- Study Together labels its attempts as `StudyTogether`.
- Smart Learn uses the engine for every attempt and only commits SRS when its retry round reaches a terminal outcome.
- Quiz creates engine evidence for questions linked to a flashcard. Manual/generated questions without `FlashcardId` remain quiz-only evidence and do not alter a card's mastery.

## Selection policy

`ILearningStatePolicy` is the single definition of due, weak, mastery, weakness, priority and recommended next action. Daily Study, Smart Learn, Study Together and Progress Insights consume this policy rather than maintaining their own thresholds.

Mode services may still apply mode-specific constraints and sequencing, such as Smart Learn retries or Daily Study quotas. They do not own the learner-state formula.

## Data model changes

`learning_attempts` adds:

- `Mode`
- nullable `StudySessionId` for standalone reviews
- `CommittedRating`
- a unique `(UserId, ClientAttemptId)` index

`flashcard_progress` adds:

- `MasteryScore`
- `WeaknessScore`
- `LastAttemptAt`
- `StateRevision`

Existing Smart Learn attempts are migrated with mode `Learn`; existing progress rows are backfilled from their aggregate review data.

## Consequences

Positive:

- An error in one mode affects review scheduling, weak-card detection and future selection everywhere.
- Quiz now contributes evidence when a reliable card link exists.
- State calculations have one implementation and can be unit-tested/versioned independently.
- Existing HTTP routes remain backward compatible.

Trade-offs:

- `FlashcardProgress` is a projection and must only be mutated through the engine.
- Self-rated modes still provide lower-quality evidence than typed recall; future policy versions can weight these differently.
- Manual quiz questions without a flashcard link cannot safely update card progress.

## Rejected alternatives

- A single large mode service was rejected because it would combine selection, UI workflow and state mutation into one class.
- Full event sourcing was rejected as unnecessary operational complexity for the current modular monolith.
- An asynchronous event bus was rejected because learner feedback and next-review state are required immediately after an answer.
