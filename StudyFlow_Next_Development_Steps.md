# StudyFlow – Next Development Steps

## 1. Trạng thái hiện tại

Foundation của dự án đã hoàn thành:

- Clean Architecture.
- Project references đúng chiều.
- React 19.
- TypeScript 6.
- Vite 8.
- React Router.
- TanStack Query.
- Zustand.
- Tailwind CSS.
- Feature-based frontend folders.
- HTTP client.
- Environment variables mẫu.
- `/api/health`.
- `BaseEntity`.
- Dependency Injection entry points.
- Architecture tests.
- Xóa code demo mặc định.

Kiểm tra:

```text
dotnet build
✓ Success

dotnet test
✓ 1/1 passed

npm run build
✓ Success

npm run lint
✓ Success

npm audit
✓ 0 vulnerabilities
```

---

# 2. Bước tiếp theo nên làm gì?

Không nên làm AI ngay.

Thứ tự nên triển khai:

```text
Foundation
    ↓
Database
    ↓
Authentication
    ↓
User
    ↓
Subject
    ↓
Study Set
    ↓
Flashcard
    ↓
Study Progress
    ↓
Spaced Repetition
    ↓
Quiz
    ↓
Dashboard
    ↓
Document Import
    ↓
AI
```

Mục tiêu tiếp theo là xây dựng được **core learning flow hoàn chỉnh mà chưa cần AI**.

---

# 3. Phase 1 – Database Infrastructure

## Mục tiêu

Thiết lập PostgreSQL và Entity Framework Core đúng kiến trúc.

## Backend cần làm

### Infrastructure

Tạo:

```text
StudyFlow.Infrastructure/
└── Persistence/
    ├── StudyFlowDbContext.cs
    ├── Configurations/
    ├── Migrations/
    └── Seed/
```

### Cài packages cần thiết

Ví dụ:

```text
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Design
Npgsql.EntityFrameworkCore.PostgreSQL
```

### DbContext

Tạo:

```text
StudyFlowDbContext
```

DbContext ban đầu có thể chưa cần toàn bộ entity.

Chỉ cần chuẩn bị infrastructure để các module sau sử dụng.

### Connection String

Development:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

Không commit password thật vào Git.

Sử dụng:

```text
appsettings.Development.json
User Secrets
Environment Variables
```

---

## Database conventions

Nên thống nhất từ đầu:

```text
Primary Key
→ Guid

CreatedAt
→ UTC

UpdatedAt
→ UTC

Delete
→ ưu tiên soft delete cho dữ liệu user quan trọng
```

Có thể mở rộng `BaseEntity`:

```text
BaseEntity

Id
CreatedAt
UpdatedAt
```

Nếu dùng soft delete:

```text
IsDeleted
DeletedAt
```

Không nhất thiết phải thêm ngay nếu chưa cần.

---

## Definition of Done

Phase này hoàn thành khi:

- PostgreSQL kết nối thành công.
- EF Core hoạt động.
- Migration tạo thành công.
- Database update thành công.
- `/api/health` vẫn hoạt động.
- Build/test không lỗi.

---

# 4. Phase 2 – Authentication & User

Đây là module business đầu tiên nên làm.

## Chức năng MVP

```text
Register
Login
Logout
Refresh Token
Get Current User
```

Sau đó mới thêm:

```text
Google Login
Forgot Password
Reset Password
Email Verification
```

---

# 5. User Entity

Domain:

```text
User
-------------------------
Id
Email
PasswordHash
DisplayName
AvatarUrl

CreatedAt
UpdatedAt
```

Không nên đưa logic framework vào Domain.

Ví dụ Domain không được phụ thuộc:

```text
ASP.NET Identity
Entity Framework Core
JWT
```

Nếu dùng ASP.NET Identity, cần thiết kế adapter/interface để không phá dependency rule của Domain.

---

# 6. Authentication Architecture

Flow:

```text
React
 ↓
POST /api/auth/login
 ↓
API
 ↓
Application
 ↓
Authentication Service
 ↓
Database
 ↓
JWT
 ↓
React
```

Token:

```text
Access Token
Refresh Token
```

Access Token nên có thời gian sống ngắn.

Refresh Token dùng để xin token mới.

---

# 7. Auth Application Structure

Ví dụ:

```text
Application/
└── Auth/
    ├── Commands/
    │   ├── Register/
    │   ├── Login/
    │   └── RefreshToken/
    │
    ├── Queries/
    │   └── GetCurrentUser/
    │
    ├── DTOs/
    └── Interfaces/
```

---

# 8. Auth API

Endpoints ban đầu:

```http
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
GET  /api/auth/me
```

Sau này:

```http
POST /api/auth/logout
POST /api/auth/google
POST /api/auth/forgot-password
POST /api/auth/reset-password
```

---

# 9. Auth Frontend

Feature:

```text
src/features/auth/
│
├── api/
├── components/
├── hooks/
├── pages/
├── schemas/
├── store/
└── types/
```

Pages:

```text
/login
/register
```

State:

```text
Current User
Authentication Status
```

Không lưu business data vào Zustand nếu TanStack Query đã quản lý được.

Zustand chỉ nên dùng cho:

```text
UI state
Auth state cần thiết
Temporary client state
```

---

# 10. Auth Definition of Done

Authentication hoàn thành khi:

- User register được.
- Email duplicate bị từ chối.
- Password validation hoạt động.
- Login đúng trả token.
- Login sai trả lỗi chuẩn.
- Protected API yêu cầu authentication.
- `/api/auth/me` trả đúng user.
- Frontend login hoạt động.
- Refresh browser vẫn giữ trạng thái hợp lý.
- Build/test/lint đều pass.

---

# 11. Phase 3 – Subject Module

Sau Auth, làm Subject trước Study Set.

## Lý do

Hierarchy chính:

```text
User
 ↓
Subject
 ↓
Study Set
 ↓
Flashcard
```

Ví dụ:

```text
User

Physics
├── Chapter 1
├── Chapter 2
└── Chapter 3
```

---

# 12. Subject Entity

```text
Subject
-------------------------
Id
UserId
Name
Description
Icon
Color

CreatedAt
UpdatedAt
```

MVP có thể đơn giản:

```text
Id
UserId
Name
Description
CreatedAt
UpdatedAt
```

---

# 13. Subject Business Rules

Quan trọng:

### Rule 1

User chỉ được xem Subject của chính mình.

### Rule 2

User chỉ được sửa/xóa Subject của chính mình.

### Rule 3

Subject Name không được rỗng.

### Rule 4

Name cần giới hạn độ dài.

Ví dụ:

```text
1–100 characters
```

### Rule 5

Nếu Subject có Study Set, cần quyết định delete behavior.

Khuyến nghị MVP:

```text
Không cho xóa Subject nếu còn Study Set
```

hoặc:

```text
Delete Subject
→ Cascade Study Sets
```

Khuyến nghị an toàn hơn:

> Không cascade destructive delete ở giai đoạn đầu.

---

# 14. Subject API

```http
GET    /api/subjects
GET    /api/subjects/{id}
POST   /api/subjects
PUT    /api/subjects/{id}
DELETE /api/subjects/{id}
```

---

# 15. Subject Frontend

Pages:

```text
/subjects
/subjects/:id
```

UI:

```text
My Subjects

Physics
Anatomy
Japanese

[Create Subject]
```

---

# 16. Subject Definition of Done

- Create Subject.
- List Subject.
- View detail.
- Edit Subject.
- Delete Subject.
- Authorization đúng owner.
- Validation đầy đủ.
- API error chuẩn.
- React Query cache invalidation đúng.
- Tests pass.

---

# 17. Phase 4 – Study Set

Study Set là collection chứa nội dung học.

Ví dụ:

```text
Physics
└── Chapter 3 – Forces
```

---

# 18. StudySet Entity

```text
StudySet
-------------------------
Id
UserId
SubjectId

Title
Description

Visibility
SourceType

CreatedAt
UpdatedAt
```

MVP có thể dùng:

```text
Visibility = Private
```

Public sharing chưa cần làm ngay.

---

# 19. Study Set Business Rules

### Ownership

Study Set phải thuộc:

```text
User
+
Subject của user đó
```

Không được gửi `SubjectId` của user khác.

### Title

Required:

```text
1–150 characters
```

### Delete

Nếu Study Set có Flashcard:

Khuyến nghị:

```text
Confirm delete
→ delete cards cùng set
```

Nhưng backend vẫn phải kiểm tra ownership.

---

# 20. Study Set API

```http
GET    /api/study-sets
GET    /api/study-sets/{id}
POST   /api/study-sets
PUT    /api/study-sets/{id}
DELETE /api/study-sets/{id}
```

Có thể hỗ trợ:

```http
GET /api/subjects/{subjectId}/study-sets
```

---

# 21. Phase 5 – Flashcard

Đây là core feature đầu tiên thực sự tạo ra giá trị.

## Flashcard Entity

```text
Flashcard
-------------------------
Id
StudySetId

FrontText
BackText

Explanation
OrderIndex

CreatedAt
UpdatedAt
```

MVP:

```text
FrontText
BackText
```

Các field sau có thể thêm sau:

```text
ImageUrl
AudioUrl
Hint
Example
Formula
```

---

# 22. Flashcard Business Rules

### Rule 1

Flashcard phải thuộc Study Set hợp lệ.

### Rule 2

User phải sở hữu Study Set.

### Rule 3

Front không được rỗng.

### Rule 4

Back không được rỗng.

### Rule 5

Không cho phép nội dung quá dài không kiểm soát.

Ví dụ:

```text
Front: max 1000 chars
Back: max 5000 chars
```

### Rule 6

OrderIndex phải được xử lý nhất quán.

---

# 23. Flashcard API

```http
GET /api/study-sets/{studySetId}/flashcards

POST /api/study-sets/{studySetId}/flashcards

PUT /api/flashcards/{id}

DELETE /api/flashcards/{id}
```

Có thể thêm bulk create:

```http
POST /api/study-sets/{studySetId}/flashcards/bulk
```

Bulk Create rất hữu ích sau này cho AI.

---

# 24. Flashcard Frontend

Study Set Detail:

```text
Chapter 3 – Forces

42 cards

[Study]
[Add Card]

--------------------------------

Newton's Second Law

F = ma

[Edit]
```

Create Form:

```text
Term / Question

Definition / Answer

[Add another]

[Save]
```

---

# 25. Phase 6 – Flashcard Study Mode

Sau khi CRUD Flashcard hoàn chỉnh, bắt đầu làm Study Mode.

Flow:

```text
Open Study Set
 ↓
Start Flashcards
 ↓
Show Front
 ↓
User recalls answer
 ↓
Reveal Back
 ↓
Rate
 ↓
Next Card
```

Buttons:

```text
Again
Hard
Good
Easy
```

Trong version đầu tiên, rating có thể chỉ lưu progress.

Chưa cần thuật toán spaced repetition phức tạp ngay.

---

# 26. Phase 7 – Flashcard Progress

Entity:

```text
FlashcardProgress
-------------------------
Id

UserId
FlashcardId

Status

CorrectCount
WrongCount

LastReviewedAt
NextReviewAt

IntervalDays

CreatedAt
UpdatedAt
```

Status:

```text
New
Learning
Reviewing
Mastered
```

---

# 27. Progress Rules

Mỗi card + user chỉ có:

```text
1 FlashcardProgress
```

Database cần unique constraint:

```text
(UserId, FlashcardId)
```

Khi user review:

```text
Again
Hard
Good
Easy
```

backend cập nhật:

```text
LastReviewedAt
NextReviewAt
CorrectCount / WrongCount
Status
Interval
```

---

# 28. Phase 8 – Spaced Repetition

Không nên hardcode toàn bộ logic trong Controller.

Tạo service riêng:

```text
ISpacedRepetitionService
```

Ví dụ:

```text
CalculateNextReview(
    currentProgress,
    reviewRating
)
```

Output:

```text
NextReviewAt
Interval
Status
```

---

# 29. Version 1 Algorithm

MVP có thể dùng thuật toán đơn giản:

```text
Again
→ +10 minutes

Hard
→ +1 day

Good
→ +3 days

Easy
→ +7 days
```

Khi đã học nhiều lần:

```text
Good
→ Interval × 2

Easy
→ Interval × 2.5

Hard
→ Interval × 1.2
```

Không cần cố clone Anki ngay.

Điều quan trọng trước tiên là:

```text
card khó
→ quay lại sớm

card dễ
→ quay lại muộn
```

---

# 30. Review API

```http
GET /api/reviews/due

POST /api/flashcards/{id}/review
```

Request:

```json
{
  "rating": "Good"
}
```

Response:

```json
{
  "nextReviewAt": "...",
  "intervalDays": 3,
  "status": "Reviewing"
}
```

---

# 31. Phase 9 – Quiz

Chỉ làm khi Flashcard Study chạy ổn.

Quiz MVP:

```text
Multiple Choice
True / False
```

Written Answer có thể làm sau vì việc chấm text phức tạp hơn.

---

# 32. Quiz Model

```text
Quiz
-------------------------
Id
StudySetId
Title
```

```text
Question
-------------------------
Id
QuizId
FlashcardId
Type
QuestionText
CorrectAnswer
```

```text
AnswerOption
-------------------------
Id
QuestionId
Text
IsCorrect
```

---

# 33. Quiz Attempt

```text
QuizAttempt
-------------------------
Id
UserId
QuizId

StartedAt
CompletedAt

CorrectCount
WrongCount
Score
```

```text
QuizAnswer
-------------------------
Id
QuizAttemptId
QuestionId

SelectedAnswer
IsCorrect
```

---

# 34. Quiz Flow

```text
Start Test
 ↓
Question 1
 ↓
Answer
 ↓
Question 2
 ↓
...
 ↓
Submit
 ↓
Result
 ↓
Wrong Answers
 ↓
Review Weak Cards
```

---

# 35. Phase 10 – Dashboard

Chỉ sau khi đã có dữ liệu học thật.

Home cần trả lời:

> Hôm nay tôi nên học gì?

UI:

```text
Good morning

Today's Review
12 cards due

[Start Review]

----------------

Recent Sets

Physics – Chapter 3
Anatomy – Bones

----------------

Today

15 min studied
32 cards reviewed
82% accuracy
```

---

# 36. Dashboard Backend

Có thể tạo:

```http
GET /api/dashboard
```

Response tổng hợp:

```text
dueCards
studyTimeToday
reviewedToday
accuracyToday
recentSets
```

Không nên để frontend gọi 10 endpoint chỉ để dựng Home.

---

# 37. Phase 11 – Study Session

Entity:

```text
StudySession
-------------------------
Id
UserId
StudySetId

Mode

StartedAt
EndedAt

CardsStudied
CorrectAnswers
WrongAnswers

DurationSeconds
```

Mode:

```text
Flashcard
Review
Quiz
Learn
```

StudySession dùng để:

- Dashboard.
- Statistics.
- Daily goal.
- Streak.
- Analytics.

---

# 38. Phase 12 – Weak Topic Detection

Chưa cần AI.

Dùng dữ liệu thật trước:

```text
WrongCount
CorrectCount
Accuracy
```

Nếu Topic chưa làm, có thể đánh giá theo Study Set.

Sau này thêm Topic:

```text
Physics
└── Chapter 3
    ├── Forces
    ├── Friction
    └── Newton's Laws
```

---

# 39. Phase 13 – Document Upload

Chỉ làm khi core study flow đã ổn.

Hỗ trợ:

```text
PDF
PPTX
DOCX
TXT
```

Flow:

```text
Upload
 ↓
Validate
 ↓
Store File
 ↓
Extract Text
 ↓
Save Document
```

Chưa cần AI ở bước đầu.

---

# 40. Document Security

Phải validate:

```text
File extension
MIME type
File size
Owner
```

Không tin:

```text
FileName
Content-Type từ client
```

File không nên được execute trên server.

---

# 41. Phase 14 – AI Integration

Lúc này mới thêm AI.

Không gọi OpenAI/Gemini trực tiếp trong Controller.

Tạo abstraction:

```csharp
public interface IAIService
{
}
```

Infrastructure implement:

```text
OpenAIService
GeminiService
```

Application chỉ phụ thuộc interface.

---

# 42. AI Use Cases

Thứ tự nên làm:

### 1. Generate Flashcards

```text
Text
→ Flashcards
```

### 2. Generate Quiz

```text
Study Set
→ Questions
```

### 3. Explain

```text
Question
→ Simple explanation
```

### 4. Generate Similar Question

```text
Wrong question
→ Similar practice question
```

### 5. Document to Study Set

```text
PDF
→ Topics
→ Flashcards
→ Quiz
```

---

# 43. AI Content Validation

AI output không được insert database trực tiếp.

Flow tốt hơn:

```text
AI Generate
 ↓
Draft
 ↓
User Preview
 ↓
Edit
 ↓
Confirm
 ↓
Save
```

Lý do:

AI có thể:

```text
Hallucinate
Generate incorrect answer
Misread document
Create duplicate cards
```

---

# 44. Backend Cross-Cutting Concerns

Trong quá trình làm các module, cần thêm dần:

```text
Validation
Global Exception Handling
Logging
Authentication
Authorization
Pagination
Rate Limiting
Caching
```

---

# 45. Error Response Standard

Nên thống nhất format API error từ sớm.

Ví dụ:

```json
{
  "code": "STUDY_SET_NOT_FOUND",
  "message": "Study set was not found.",
  "errors": null
}
```

Validation:

```json
{
  "code": "VALIDATION_ERROR",
  "message": "Validation failed.",
  "errors": {
    "title": [
      "Title is required."
    ]
  }
}
```

---

# 46. Result Pattern

Có thể sử dụng:

```text
Result<T>
```

Application trả:

```text
Success
Failure
NotFound
Conflict
Forbidden
```

Không nên throw exception cho mọi business case.

---

# 47. Validation

Khuyến nghị:

```text
FluentValidation
```

Ví dụ:

```text
CreateSubjectValidator

Name
Required
MaxLength(100)
```

Validation nên nằm gần Application use case.

---

# 48. Testing Strategy

Hiện tại mới có architecture test.

Tiếp theo nên thêm:

```text
Unit Tests
Integration Tests
API Tests
```

Priority:

### Unit

```text
Spaced Repetition
Business Rules
Validators
```

### Integration

```text
Database
Repository
Auth
Ownership
```

### API

```text
Register
Login
Create Subject
Create Study Set
Create Flashcard
```

---

# 49. Test Naming

Ví dụ:

```text
CreateSubject_WhenNameIsEmpty_ShouldFail

DeleteStudySet_WhenUserIsNotOwner_ShouldReturnForbidden

ReviewFlashcard_WhenRatingIsGood_ShouldScheduleNextReview
```

---

# 50. Database Indexes

Đừng thêm index ngẫu nhiên.

Các index có khả năng cần:

```text
Users.Email
Subjects.UserId
StudySets.SubjectId
StudySets.UserId
Flashcards.StudySetId
FlashcardProgress.UserId
FlashcardProgress.NextReviewAt
```

Unique:

```text
Users.Email

FlashcardProgress
(UserId, FlashcardId)
```

---

# 51. API Authorization Rule

Không bao giờ chỉ kiểm tra:

```text
entity.Id
```

Phải kiểm tra:

```text
entity.UserId == CurrentUserId
```

Ví dụ:

```text
GET /api/study-sets/{id}
```

Không được trả Study Set nếu `id` tồn tại nhưng thuộc user khác.

---

# 52. Recommended Git Workflow

Mỗi feature nên tách branch:

```text
feature/auth
feature/subjects
feature/study-sets
feature/flashcards
feature/spaced-repetition
feature/quiz
```

Không nên làm tất cả trong một branch lớn.

Commit:

```text
feat(auth): add user registration

feat(subjects): add subject CRUD

test(subjects): add ownership integration tests
```

---

# 53. Immediate Next Sprint

Đây là phần nên làm NGAY sau foundation.

## Sprint 1

### Backend

- [ ] Cấu hình PostgreSQL.
- [ ] Thêm EF Core.
- [ ] Tạo `StudyFlowDbContext`.
- [ ] Tạo migration đầu tiên.
- [ ] Tạo User entity.
- [ ] Register.
- [ ] Login.
- [ ] JWT.
- [ ] Refresh Token.
- [ ] Current User.
- [ ] Global exception handling.
- [ ] Validation.

### Frontend

- [ ] Login Page.
- [ ] Register Page.
- [ ] Auth API client.
- [ ] Protected Routes.
- [ ] Current User query/state.
- [ ] Handle 401.
- [ ] Refresh token strategy.

### Testing

- [ ] Auth unit/integration tests.
- [ ] Unauthorized endpoint test.
- [ ] Invalid login test.
- [ ] Duplicate email test.

---

# 54. Sprint 2

## Subject

- [ ] Subject entity.
- [ ] EF configuration.
- [ ] Migration.
- [ ] Create Subject.
- [ ] List Subject.
- [ ] Get Subject.
- [ ] Update Subject.
- [ ] Delete Subject.
- [ ] Ownership checks.
- [ ] Subject page.
- [ ] Create Subject modal/form.
- [ ] Tests.

---

# 55. Sprint 3

## Study Set

- [ ] StudySet entity.
- [ ] Subject relationship.
- [ ] CRUD API.
- [ ] Ownership.
- [ ] Study Set list.
- [ ] Study Set detail.
- [ ] Create/Edit form.
- [ ] Tests.

---

# 56. Sprint 4

## Flashcard

- [ ] Flashcard entity.
- [ ] CRUD.
- [ ] Bulk create.
- [ ] Flashcard editor.
- [ ] Flashcard player.
- [ ] Study flow.
- [ ] Tests.

---

# 57. Sprint 5

## Progress + Spaced Repetition

- [ ] FlashcardProgress entity.
- [ ] Review rating.
- [ ] Review algorithm.
- [ ] Due cards API.
- [ ] Review page.
- [ ] Progress tests.

Đây là sprint biến project từ:

```text
Flashcard CRUD App
```

thành:

```text
Learning App
```

---

# 58. Sprint 6

## Quiz

- [ ] Quiz entity.
- [ ] Questions.
- [ ] Answer options.
- [ ] Attempt.
- [ ] Result.
- [ ] Wrong-answer review.
- [ ] Tests.

---

# 59. Sprint 7

## Dashboard

- [ ] StudySession.
- [ ] Daily statistics.
- [ ] Recent Study Sets.
- [ ] Due Review.
- [ ] Accuracy.
- [ ] Study time.
- [ ] Dashboard UI.

---

# 60. Khi nào MVP được xem là hoàn thành?

Một user mới có thể thực hiện toàn bộ flow:

```text
Register
 ↓
Login
 ↓
Create Physics
 ↓
Create Chapter 1
 ↓
Add 20 Flashcards
 ↓
Study Flashcards
 ↓
Rate Cards
 ↓
Leave App
 ↓
Come Back Tomorrow
 ↓
See Cards Due
 ↓
Review
 ↓
Take Quiz
 ↓
See Wrong Answers
 ↓
See Progress
```

Nếu flow này chạy tốt:

> **Core MVP đã thành công.**

Chưa cần AI.

---

# 61. Việc nên làm ngay bây giờ

Task tiếp theo được khuyến nghị:

```text
DATABASE + AUTHENTICATION
```

Cụ thể:

```text
1. PostgreSQL
2. EF Core
3. DbContext
4. User Entity
5. Migration
6. Register
7. Login
8. JWT
9. Refresh Token
10. Current User
11. Auth Frontend
12. Auth Tests
```

Sau khi Authentication hoàn thành:

```text
Subject
→ Study Set
→ Flashcard
```

Không nên làm song song quá nhiều module.

---

# 62. Nguyên tắc quan trọng trong giai đoạn tiếp theo

## 1. Vertical Slice

Hoàn thành từng feature từ:

```text
Database
→ Domain
→ Application
→ API
→ Frontend
→ Test
```

rồi mới sang feature tiếp theo.

## 2. Business logic không nằm trong Controller

Controller chỉ:

```text
Receive Request
→ Call Application
→ Return Response
```

## 3. Frontend không chứa business rule quan trọng

Các rule như:

```text
Ownership
Next Review
Quiz score
Permission
```

phải được enforce ở backend.

## 4. Không optimize quá sớm

Chưa cần:

```text
Microservices
Kafka
Kubernetes
Event Sourcing
CQRS phức tạp
```

cho MVP.

## 5. Core Learning trước AI

Thứ tự:

```text
Flashcard
→ Recall
→ Review
→ Progress
→ Quiz
→ AI
```

AI là feature tăng tốc trải nghiệm, không phải nền móng của hệ thống.

---

# 63. Target Architecture Sau Core MVP

```text
React Client
     │
     │ HTTPS
     ↓
ASP.NET Core API
     │
     ├── Application
     │
     ├── Domain
     │
     └── Infrastructure
             │
             ├── PostgreSQL
             ├── Redis
             ├── Object Storage
             └── AI Provider
```

---

# 64. Kết luận

Bạn đã hoàn thành:

```text
Project Foundation
```

Bây giờ không nên tiếp tục thêm framework hoặc package nếu chưa có nhu cầu.

Hướng phát triển đúng:

```text
Architecture
✓

Database
→ NEXT

Authentication
→ NEXT

Subject
→

Study Set
→

Flashcard
→

Spaced Repetition
→

Quiz
→

Progress
→

AI
```

**Task thực tế tiếp theo:**

> Hoàn thiện PostgreSQL + EF Core + Authentication end-to-end trước khi bắt đầu Subject và Study Set.
