# StudyFlow

Live Battle V1: xem [hướng dẫn chơi, kiến trúc và kiểm thử](docs/LIVE_BATTLE.md). Tạo phòng từ **Study Set → Live Battle**, hoặc vào phòng qua **Battle** / `/join`.

StudyFlow is a learning platform focused on flashcards, quizzes, spaced repetition, and progress tracking. This repository is the foundation for the MVP in `StudyFlow_Project_Overview.md`.

## Structure

```text
src/
├── backend/
│   ├── StudyFlow.API/            # HTTP boundary and composition root
│   ├── StudyFlow.Application/    # Use cases and contracts
│   ├── StudyFlow.Domain/         # Entities and business rules
│   └── StudyFlow.Infrastructure/ # Database and integrations
└── frontend/                     # React feature-based application
tests/StudyFlow.Tests/
```

Dependency direction: `API -> Infrastructure -> Application -> Domain`.

## Run Sprint 1

```powershell
docker compose up -d postgres
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/backend/StudyFlow.Infrastructure --startup-project src/backend/StudyFlow.API
dotnet run --project src/backend/StudyFlow.API

Set-Location src/frontend
npm install
npm run dev
```

## Verify

```powershell
dotnet test StudyFlow.sln
Set-Location src/frontend
npm run lint
npm run build
```

Development PostgreSQL listens on port `5433` to avoid colliding with a local PostgreSQL installation. The committed credentials and JWT key are development-only; configure production secrets with environment variables or .NET User Secrets.

Sprint 1 includes PostgreSQL persistence, JWT authentication, rotating refresh tokens in an HttpOnly cookie, current-user lookup, validation, consistent API errors, protected frontend routes, and auth integration tests.

Sprint 2 adds owner-scoped Subject CRUD, EF configuration and migration, validation, `/subjects` and `/subjects/:id` pages, React Query cache invalidation, and integration coverage for CRUD and ownership isolation.

Sprint 3 adds StudySet CRUD using only `SubjectId`. Ownership is derived through `StudySet -> Subject -> User`; no duplicated `UserId`, visibility, or source metadata is stored. Subjects with study sets cannot be deleted. Study set lists live within `/subjects/:id`, with details at `/study-sets/:id`.

Sprint 4 adds owner-scoped Flashcard CRUD and atomic bulk creation, stable per-set ordering, optional explanations, database cascade from StudySet to its cards, an inline card editor, and a recall player at `/study/:id/flashcards`. Ratings and persisted learning progress remain Sprint 5 and are intentionally not included.

Sprint 5 adds one progress record per `(UserId, FlashcardId)`, review ratings, a standalone spaced-repetition scheduler, due-card queries, automatic scheduling, and review UI. The set player now saves ratings and advances automatically; `/reviews/due` provides the current user's scheduled queue. Quiz remains outside this sprint.

Sprint 6 adds server-generated Multiple Choice and True/False quizzes from existing flashcards, protected attempts, mutable answers before submission, immutable completed attempts, server-side scoring, and wrong-answer review. Attempt payloads never expose correctness before completion. Quiz creation and entry points live within each StudySet, with test-taking at `/quizzes/:id`.

Sprint 7 adds persisted study sessions for flashcard reviews and completed quizzes, owner-scoped session endpoints, and a dashboard at `/home`. `GET /api/dashboard` reports due cards, today's UTC study time, cards reviewed, quiz questions, combined accuracy, and the five most recently studied sets. Empty or unfinished sessions are excluded from analytics; quiz sessions are created by the backend when an attempt is completed.

Sprint 8 adds owner-scoped progress analytics at `/progress`, per-Study Set progress summaries, flashcard status distribution, weighted quiz accuracy, weak-card detection, and recommended review counts. Weakness is inferred from existing review history (`WrongCount`, `CorrectCount`, `EaseFactor`, `Status`, and `IntervalDays`); no Topic, streak, long-term chart, notification, import, or AI model is introduced.

Sprint 9 adds UTC-calendar study streaks and learning history derived from completed, non-empty Study Sessions. `GET /api/progress/streak` returns current and longest streaks plus total active days; `GET /api/progress/history?days=30` returns a gap-free daily series for 7–365 days. The progress UI includes 30/90-day study-time visualization and aggregate activity totals. No new persistence table or migration is required.

Technical debt: streak and daily history currently use UTC calendar dates. Add a user preference such as an IANA `Timezone` before implementing local-time reminders or notifications.

Sprint 10 adds authenticated PDF, DOCX, PPTX, and UTF-8 TXT import with a 20 MB limit, extension/MIME/signature validation, local file storage behind `IFileStorageService`, synchronous text extraction, processing status, metadata, preview, listing, and deletion of both metadata and physical files. Files are stored outside the public web root under generated storage keys; PostgreSQL stores metadata and extracted text, never the source binary. Document and optional Subject/StudySet ownership are enforced by the backend. The `/documents` UI provides upload, status, preview, error, and deletion states. No AI call is made.

Sprint 11 adds Gemini-backed, source-grounded generation behind `IAIService`. It creates reviewable AI drafts for flashcards and quizzes, supports document-grounded explanations and similar questions, enforces ownership and limits, validates all model output on the backend, and persists `AIJob` lifecycle/error data. Drafts are never copied into learning content until the user explicitly saves them.

Configure Gemini locally with .NET User Secrets (PowerShell, from the repository root):

```powershell
dotnet user-secrets init --project src/backend/StudyFlow.API
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY" --project src/backend/StudyFlow.API
```

The configured provider model is `gemini-3.5-flash-lite`. Never commit the API key to `appsettings.json`.

Smart Learn adds an adaptive retrieval session at `/study/:studySetId/learn`. The backend prioritizes due, weak, new, and previously missed cards; new material can start with recognition before moving to typed recall. Learners answer before revealing, record confidence, receive feedback, and retry missed cards after several intervening items. Every response is stored in `learning_attempts` with its attempt type, result, confidence, response time, hint usage, and reveal state. Optional two-level Gemini hints guide without exposing the final answer and require a completed source document attached to the Study Set.

Foreign-language cards can optionally store a BCP 47 language code (for example `ja-JP` or `en-US`), reading text, and romanization. Manual, bulk, and AI-draft editors support these fields. Study and due-review flows reveal the prompt, reading, meaning, then rating in separate stages. Pronunciation uses the browser/device Web Speech API with normal, slow, and optional auto-play modes, so playing audio does not consume Gemini requests. Voice availability and quality depend on the browser and installed operating-system voices.

Daily Study at `/study/today` creates a cross-library plan for 5, 10, 15, or unlimited minutes. It balances due, weak, and new cards, supports written recall with typo-tolerant comparison, and rotates language cards through listening, reverse recall, and meaning recall. Flashcards can also store source-grounded example text, an example translation, a memory tip, and newline-separated accepted answers; these fields are editable manually and in AI drafts and appear as feedback after recall.

Each flashcard can have one replaceable JPG, PNG, or WEBP image up to 5 MB. The API validates extension, MIME type, and file signature, stores the binary through `IFileStorageService`, and serves it only through an authenticated owner-scoped endpoint. Replacing or deleting an image, flashcard, or its Study Set also removes the corresponding physical file.

The frontend uses a mobile-first StudyFlow visual system with an ambient blue surface, compact top bar, fixed bottom navigation, touch-friendly study controls, and responsive desktop grids. English and Vietnamese can be switched from the shared language control; the selection persists in `localStorage` under `studyflow-language`.

## Social Network

Profiles, private-by-default publication, discovery, friendships, comments, independent remixes, friend-only messaging, and Battle invitations are available. See [Social Network guide](docs/SOCIAL_NETWORK.md) for usage, privacy rules, moderation configuration, architecture, and verification commands.

Email ownership is verified with a six-digit OTP before new accounts can sign in. Configure the Gmail SMTP sender using [the Email OTP guide](docs/EMAIL_OTP.md).
