# Study App – Tổng Quan Cấu Trúc Dự Án

## 1. Giới thiệu dự án

### 1.1. Tên dự án tạm thời
**StudyFlow**  
Tên có thể thay đổi sau này.

### 1.2. Mục tiêu
Xây dựng một ứng dụng hỗ trợ người dùng:

- Tự học hiệu quả.
- Ghi nhớ kiến thức nhanh hơn.
- Ôn tập đúng thời điểm.
- Theo dõi các nội dung đã nhớ và chưa nhớ.
- Tự động tạo bài học từ tài liệu.
- Giảm thời gian chuẩn bị flashcard và câu hỏi.
- Tập trung ôn lại các phần kiến thức yếu.
- Chuẩn bị cho bài kiểm tra hoặc kỳ thi.

Ứng dụng lấy cảm hứng từ:

- Quizlet
- Anki
- AI Tutor

Nhưng tập trung vào trải nghiệm:

> **Upload tài liệu → Học → Làm bài → Phát hiện điểm yếu → Ôn lại đúng lúc → Ghi nhớ lâu hơn**

---

# 2. Đối tượng người dùng

## 2.1. Người dùng chính

- Học sinh.
- Sinh viên.
- Người học ngoại ngữ.
- Người tự học chứng chỉ.
- Người cần ghi nhớ nhiều kiến thức.
- Người chuẩn bị cho kỳ thi.

## 2.2. Ví dụ nhu cầu

Người dùng có thể:

- Tạo flashcard môn Physics.
- Upload slide bài giảng.
- Paste ghi chú.
- Tạo quiz từ tài liệu.
- Ôn lại các câu đã sai.
- Theo dõi tiến độ học.
- Lập lịch ôn trước kỳ thi.
- Nhờ AI giải thích một khái niệm khó hiểu.

---

# 3. Giá trị cốt lõi

Ứng dụng không chỉ là nơi lưu flashcard.

Hệ thống cần giúp người dùng trả lời ba câu hỏi:

1. **Tôi cần học gì?**
2. **Tôi đang yếu phần nào?**
3. **Khi nào tôi cần ôn lại?**

Vòng lặp học tập chính:

```text
Tài liệu
   ↓
Phân tích kiến thức
   ↓
Flashcard / Quiz
   ↓
Người dùng học
   ↓
Ghi nhận kết quả
   ↓
Phân tích điểm yếu
   ↓
Spaced Repetition
   ↓
Ôn lại
   ↓
Mastered
```

---

# 4. Phạm vi MVP

Phiên bản đầu tiên nên tập trung vào chức năng học cá nhân.

## MVP bao gồm

- Đăng ký.
- Đăng nhập.
- Google Login.
- Tạo môn học.
- Tạo Study Set.
- Tạo flashcard thủ công.
- Học flashcard.
- Làm quiz.
- Lưu kết quả.
- Theo dõi câu đúng/sai.
- Spaced Repetition.
- Dashboard học tập.
- Theo dõi progress.
- Search Study Set cá nhân.

## Chưa cần trong MVP

- Marketplace.
- Chat giữa người dùng.
- Leaderboard lớn.
- Follow người dùng.
- Comment.
- Classroom.
- Teacher dashboard.
- Payment.
- Subscription.
- Social feed.

Những chức năng trên nên phát triển sau khi core learning system hoạt động ổn định.

---

# 5. Các module chính

```text
StudyFlow
│
├── Authentication
│
├── User Profile
│
├── Subject
│
├── Study Set
│
├── Flashcard
│
├── Learn Mode
│
├── Quiz
│
├── Study Session
│
├── Spaced Repetition
│
├── Progress Tracking
│
├── Exam Planning
│
├── AI Learning
│
├── Document Import
│
└── Notification
```

---

# 6. Authentication Module

## Chức năng

- Register.
- Login.
- Logout.
- Refresh Token.
- Forgot Password.
- Reset Password.
- Google Login.
- Email Verification.

## User Role

Ban đầu chỉ cần:

```text
User
Admin
```

Sau này có thể thêm:

```text
Teacher
Student
Moderator
```

---

# 7. User Profile Module

Thông tin người dùng:

```text
User
-------------------------
Id
Email
PasswordHash
DisplayName
AvatarUrl
DateOfBirth
Timezone
Language
CreatedAt
UpdatedAt
```

User có thể cấu hình:

- Daily study goal.
- Daily reminder.
- Preferred study time.
- Interface language.
- Study session duration.

Ví dụ:

```text
Daily Goal: 30 minutes

Reminder:
20:00 every day
```

---

# 8. Subject Module

Subject đại diện cho một môn học.

Ví dụ:

```text
Physics 1030
Anatomy
Japanese N5
.NET
SQL
```

Cấu trúc:

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

Quan hệ:

```text
User
 └── Subject
      └── StudySet
```

---

# 9. Study Set Module

Study Set là một nhóm kiến thức.

Ví dụ:

```text
Physics 1030
    ├── Chapter 1
    ├── Chapter 2
    └── Chapter 3
```

Database:

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

Visibility:

```text
Private
Public
Unlisted
```

SourceType:

```text
Manual
Document
AI
Imported
```

---

# 10. Flashcard Module

Một flashcard gồm:

```text
Front
Back
```

Ví dụ:

```text
Front:
What is acceleration?

Back:
The rate of change of velocity over time.
```

Database:

```text
Flashcard
-------------------------
Id
StudySetId
FrontText
BackText
Explanation
ImageUrl
OrderIndex
CreatedAt
UpdatedAt
```

Sau này có thể thêm:

- Audio.
- Image.
- Formula.
- Example.
- Hint.

---

# 11. Flashcard Study Mode

Flow:

```text
Flashcard
    ↓
Show Question
    ↓
User thinks
    ↓
Reveal Answer
    ↓
Rate memory
```

Rating:

```text
Again
Hard
Good
Easy
```

Ví dụ:

```text
What is centripetal acceleration?

[Show Answer]

How well did you remember?

[Again] [Hard] [Good] [Easy]
```

---

# 12. Spaced Repetition Module

Đây là một trong những module quan trọng nhất.

Mục tiêu:

> Câu khó xuất hiện thường xuyên hơn, câu đã nhớ xuất hiện ít hơn.

Ví dụ đơn giản:

```text
Again
→ 10 minutes

Hard
→ 1 day

Good
→ 3 days

Easy
→ 7 days
```

Sau nhiều lần học:

```text
1 day
↓
3 days
↓
7 days
↓
14 days
↓
30 days
↓
60 days
```

Database:

```text
FlashcardProgress
-------------------------
Id
UserId
FlashcardId

Status

CorrectCount
WrongCount

Difficulty

LastReviewedAt
NextReviewAt

IntervalDays
EaseFactor

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

# 13. Quiz Module

Quiz có thể được tạo từ Study Set.

Các loại câu hỏi:

```text
Multiple Choice
True / False
Written Answer
Matching
Fill in the Blank
```

MVP nên ưu tiên:

```text
Multiple Choice
True / False
Written Answer
```

Database:

```text
Quiz
-------------------------
Id
StudySetId
Title
QuestionCount
CreatedAt
```

```text
Question
-------------------------
Id
QuizId
FlashcardId
QuestionType
QuestionText
Explanation
Difficulty
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

# 14. Quiz Attempt

Mỗi lần người dùng làm quiz cần lưu lại.

```text
QuizAttempt
-------------------------
Id
UserId
QuizId
StartedAt
CompletedAt

Score
CorrectCount
WrongCount
DurationSeconds
```

Chi tiết câu trả lời:

```text
QuizAnswer
-------------------------
Id
QuizAttemptId
QuestionId

UserAnswer
IsCorrect

AnsweredAt
TimeSpentSeconds
```

---

# 15. Weak Topic Detection

Một trong những điểm khác biệt chính của app.

Hệ thống phân tích:

```text
Correct Answer
Wrong Answer
Response Time
Confidence
Number of Attempts
```

Sau đó tính mức độ hiểu.

Ví dụ:

```text
Physics

Kinematics
██████████ 90%

Acceleration
████████░░ 80%

Velocity Graph
█████░░░░░ 50%

Projectile Motion
███░░░░░░░ 30%
```

App sẽ đề xuất:

```text
You should review:

1. Projectile Motion
2. Velocity Graph
```

---

# 16. Topic Module

Để phân tích điểm yếu chính xác hơn, mỗi flashcard/question có thể thuộc Topic.

Ví dụ:

```text
Physics

Chapter 2
    ├── Velocity
    ├── Acceleration
    ├── Graphs
    └── Kinematic Equations
```

Database:

```text
Topic
-------------------------
Id
StudySetId
Name
Description
```

Quan hệ:

```text
Topic
 ├── Flashcards
 └── Questions
```

---

# 17. Study Session

Mỗi lần người dùng bắt đầu học được xem là một Study Session.

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
QuestionsAnswered
CorrectAnswers

DurationSeconds
```

Mode:

```text
Flashcard
Learn
Quiz
Review
ExamPractice
```

---

# 18. Learn Mode

Learn Mode là chế độ học tự động.

Hệ thống quyết định câu tiếp theo dựa vào:

```text
New cards
+
Due cards
+
Wrong cards
+
Weak topics
```

Ví dụ:

```text
Learning Session

5 New Cards
8 Review Cards
4 Weak Topic Questions

Estimated Time:
15 minutes
```

Flow:

```text
Start Learn
    ↓
Easy Question
    ↓
Medium Question
    ↓
Wrong?
    ↓
Explain
    ↓
Ask Similar Question
    ↓
Continue
```

---

# 19. Smart Review

Home page hiển thị:

```text
Today's Review

12 cards due
5 weak questions

Estimated:
10 minutes

[Start Review]
```

Ưu tiên:

```text
1. Overdue cards
2. Wrong answers
3. Weak topics
4. New cards
```

---

# 20. Progress Tracking

Dashboard có thể hiển thị:

```text
Today

Study Time
32 min

Cards Reviewed
45

Questions
30

Accuracy
83%
```

Weekly:

```text
Mon  25 min
Tue  40 min
Wed  0
Thu  32 min
Fri  55 min
```

Progress:

```text
New:       32
Learning:  18
Reviewing: 45
Mastered:  80
```

---

# 21. Streak System

Streak giúp tăng động lực.

```text
7 Day Streak 🔥
```

Điều kiện:

User đạt Daily Goal.

Ví dụ:

```text
Daily Goal

20 cards
OR
15 minutes studying
```

Database:

```text
UserStreak
-------------------------
UserId

CurrentStreak
LongestStreak

LastStudyDate
```

---

# 22. Exam Mode

User có thể tạo kỳ thi.

```text
Exam
-------------------------
Physics Midterm

Exam Date:
2026-09-25
```

Database:

```text
Exam
-------------------------
Id
UserId
SubjectId

Name
ExamDate

CreatedAt
```

App tính:

```text
14 days remaining
```

Sau đó tạo kế hoạch:

```text
Day 1
Chapter 1

Day 2
Chapter 2

Day 3
Chapter 3

Day 4
Weak Topics

...

Day 13
Mock Test

Day 14
Final Review
```

---

# 23. AI Module

AI không nên thay thế hệ thống học.

AI chỉ hỗ trợ:

```text
Generate
Explain
Summarize
Analyze
```

AI features:

## Generate Flashcards

Input:

```text
Lecture Notes
```

Output:

```text
20 Flashcards
```

## Generate Quiz

```text
Generate:

10 Easy
10 Medium
5 Hard
```

## Explain Answer

Nếu người dùng sai:

```text
Explain this simply.
```

AI có thể giải thích:

```text
Imagine a car driving around a circular track...
```

## Similar Question

Sau câu sai:

```text
Generate another question
testing the same concept.
```

---

# 24. Document Import

Nguồn tài liệu:

```text
PDF
PPTX
DOCX
TXT
Paste Text
```

Flow:

```text
Upload Document
      ↓
Extract Text
      ↓
Clean Content
      ↓
Split Content
      ↓
AI Analyze
      ↓
Detect Topics
      ↓
Generate Flashcards
      ↓
Generate Quiz
```

Database:

```text
Document
-------------------------
Id
UserId
SubjectId
StudySetId

FileName
FileType
FileUrl

ProcessingStatus

CreatedAt
```

Status:

```text
Uploaded
Processing
Completed
Failed
```

---

# 25. AI Job

AI processing nên chạy theo job.

```text
AIJob
-------------------------
Id
UserId

JobType
Status

InputReference
OutputReference

CreatedAt
CompletedAt
```

JobType:

```text
GenerateFlashcards
GenerateQuiz
SummarizeDocument
ExplainQuestion
AnalyzeWeakTopics
```

---

# 26. Notification Module

Thông báo ví dụ:

```text
You have 12 cards due today.
```

Hoặc:

```text
Physics exam is in 5 days.
```

Notification types:

```text
ReviewReminder
StudyReminder
ExamReminder
StreakReminder
SystemNotification
```

---

# 27. Home Page

Trang Home nên trả lời:

> Tôi nên học gì ngay bây giờ?

Layout đề xuất:

```text
----------------------------------

Good morning 👋

🔥 7 day streak

Today's Goal
███████░░░
18 / 25 minutes

----------------------------------

Review Due

12 Flashcards
5 Questions

[Start Review]

----------------------------------

Weak Topics

Projectile Motion    45%
Velocity Graph       60%

[Practice]

----------------------------------

Your Subjects

Physics
Anatomy
Japanese
SQL

----------------------------------

Recent Study Sets

Chapter 3 – Forces
Chapter 4 – Energy

----------------------------------
```

---

# 28. Library Page

Library quản lý:

```text
Subjects
Study Sets
Documents
Folders
```

Ví dụ:

```text
Physics
│
├── Chapter 1
├── Chapter 2
├── Chapter 3
└── Midterm Review
```

---

# 29. Study Set Detail Page

Ví dụ:

```text
Chapter 3 – Forces

42 Flashcards
25 Questions

Progress
████████░░ 78%

[Flashcards]

[Learn]

[Test]

[Review]

----------------------------------

Terms

Newton's First Law
Newton's Second Law
Newton's Third Law

----------------------------------
```

---

# 30. Suggested Frontend Pages

```text
/
│
├── /
├── /login
├── /register
│
├── /home
│
├── /library
│
├── /subjects
├── /subjects/:id
│
├── /study-sets
├── /study-sets/:id
│
├── /study-sets/:id/edit
│
├── /study/:id/flashcards
├── /study/:id/learn
├── /study/:id/test
├── /study/:id/review
│
├── /progress
├── /exam
├── /documents
│
├── /profile
└── /settings
```

---

# 31. Frontend Structure

Đề xuất:

```text
src/
│
├── api/
│
├── assets/
│
├── components/
│
├── features/
│   │
│   ├── auth/
│   ├── subjects/
│   ├── studySets/
│   ├── flashcards/
│   ├── quizzes/
│   ├── study/
│   ├── progress/
│   ├── exams/
│   └── ai/
│
├── hooks/
│
├── layouts/
│
├── pages/
│
├── routes/
│
├── services/
│
├── store/
│
├── types/
│
├── utils/
│
└── App.tsx
```

---

# 32. Backend Architecture

Khuyến nghị sử dụng:

```text
ASP.NET Core Web API
```

Architecture:

```text
API
 ↓
Application
 ↓
Domain
 ↓
Infrastructure
 ↓
Database
```

Có thể sử dụng Clean Architecture.

---

# 33. Backend Project Structure

```text
StudyFlow/
│
├── StudyFlow.API/
│
├── StudyFlow.Application/
│
├── StudyFlow.Domain/
│
├── StudyFlow.Infrastructure/
│
└── StudyFlow.Tests/
```

---

# 34. Domain Layer

Domain chứa:

```text
Entities
Enums
ValueObjects
Domain Rules
```

Ví dụ:

```text
Domain/
│
├── Entities/
│   ├── User.cs
│   ├── Subject.cs
│   ├── StudySet.cs
│   ├── Flashcard.cs
│   ├── Quiz.cs
│   ├── Question.cs
│   ├── StudySession.cs
│   └── FlashcardProgress.cs
│
└── Enums/
```

---

# 35. Application Layer

Chứa use cases.

Ví dụ:

```text
Application/
│
├── Auth/
│
├── Subjects/
│
├── StudySets/
│
├── Flashcards/
│
├── Quizzes/
│
├── StudySessions/
│
├── Progress/
│
├── Exams/
│
└── AI/
```

---

# 36. Infrastructure Layer

Chứa:

```text
Database
Repository
External Services
AI Provider
Email
File Storage
Cache
```

Ví dụ:

```text
Infrastructure/
│
├── Persistence/
│
├── Authentication/
│
├── AI/
│
├── Email/
│
├── Storage/
│
└── Caching/
```

---

# 37. API Layer

Chứa Controller / Endpoint.

```text
Controllers/
│
├── AuthController
├── SubjectsController
├── StudySetsController
├── FlashcardsController
├── QuizController
├── StudyController
├── ProgressController
├── ExamsController
├── DocumentsController
└── AIController
```

---

# 38. API Design

Ví dụ API:

## Authentication

```http
POST /api/auth/register

POST /api/auth/login

POST /api/auth/google

POST /api/auth/refresh-token

POST /api/auth/forgot-password
```

## Subjects

```http
GET /api/subjects

POST /api/subjects

GET /api/subjects/{id}

PUT /api/subjects/{id}

DELETE /api/subjects/{id}
```

## Study Sets

```http
GET /api/study-sets

POST /api/study-sets

GET /api/study-sets/{id}

PUT /api/study-sets/{id}

DELETE /api/study-sets/{id}
```

## Flashcards

```http
GET /api/study-sets/{id}/flashcards

POST /api/study-sets/{id}/flashcards

PUT /api/flashcards/{id}

DELETE /api/flashcards/{id}
```

## Study

```http
POST /api/study/session

POST /api/study/flashcards/{id}/review

GET /api/study/due

GET /api/study/recommended
```

## Quiz

```http
POST /api/quizzes

GET /api/quizzes/{id}

POST /api/quizzes/{id}/attempt

POST /api/quiz-attempts/{id}/answer

POST /api/quiz-attempts/{id}/complete
```

---

# 39. Database Relationship Overview

```text
User
│
├── Subjects
│   │
│   └── StudySets
│       │
│       ├── Flashcards
│       ├── Topics
│       ├── Quizzes
│       └── Documents
│
├── FlashcardProgress
│
├── QuizAttempts
│
├── StudySessions
│
├── Exams
│
└── Notifications
```

---

# 40. Tech Stack

## Frontend

```text
React
TypeScript
Vite
Tailwind CSS
React Router
TanStack Query
Zustand
```

## Backend

```text
ASP.NET Core Web API
Entity Framework Core
FluentValidation
AutoMapper or Mapster
JWT Authentication
```

## Database

```text
PostgreSQL
```

## Cache

```text
Redis
```

## AI

Có thể sử dụng:

```text
OpenAI API
Gemini API
```

Nên tạo abstraction:

```text
IAIService
```

để có thể đổi provider sau này.

## Storage

Có thể sử dụng:

```text
AWS S3
Cloudinary
Azure Blob Storage
```

---

# 41. Security

Cần đảm bảo:

- Password hash.
- JWT expiration.
- Refresh token rotation.
- Rate limiting.
- Authorization.
- File validation.
- File size limit.
- Input validation.
- XSS protection.
- SQL Injection protection.
- Không expose API key ở frontend.

---

# 42. Business Rules Quan Trọng

## Rule 1

User chỉ được sửa dữ liệu của chính mình.

## Rule 2

Private Study Set không được truy cập bởi user khác.

## Rule 3

Flashcard Progress thuộc riêng từng user.

## Rule 4

Một lần review phải cập nhật NextReviewAt.

## Rule 5

Quiz Attempt đã Completed không được sửa câu trả lời.

## Rule 6

AI generated content cần được user kiểm tra trước khi lưu chính thức.

## Rule 7

Document processing thất bại không được tạo Study Set rỗng.

## Rule 8

Study Session chỉ tính khi có hoạt động học thật.

---

# 43. Suggested Development Phases

## Phase 1 – Foundation

```text
Authentication
User
Database
Project Architecture
```

## Phase 2 – Core Content

```text
Subject
Study Set
Flashcard
Library
```

## Phase 3 – Learning

```text
Flashcard Study
Quiz
Study Session
Progress
```

## Phase 4 – Memory System

```text
Spaced Repetition
Due Review
Weak Topics
Smart Review
```

## Phase 5 – AI

```text
AI Generate Flashcard
AI Generate Quiz
AI Explanation
```

## Phase 6 – Documents

```text
PDF
PPTX
DOCX
AI Document Processing
```

## Phase 7 – Advanced Learning

```text
Exam Mode
Study Plan
Statistics
Streak
Notifications
```

## Phase 8 – Social

Sau này mới làm:

```text
Public Study Set
Search Public Set
Clone Study Set
Follow
Class
Leaderboard
```

---

# 44. MVP User Flow

```text
Register
   ↓
Login
   ↓
Create Subject
   ↓
Create Study Set
   ↓
Add Flashcards
   ↓
Study Flashcards
   ↓
Rate Memory
   ↓
Save Progress
   ↓
Take Quiz
   ↓
Detect Wrong Answers
   ↓
Review Weak Cards
   ↓
Repeat
```

---

# 45. Future User Flow With AI

```text
Upload Lecture PDF
       ↓
AI reads document
       ↓
Detect chapters/topics
       ↓
Generate summary
       ↓
Generate flashcards
       ↓
Generate quiz
       ↓
User reviews AI content
       ↓
Start learning
       ↓
Track performance
       ↓
Detect weaknesses
       ↓
Generate targeted questions
       ↓
Smart review
```

---

# 46. Core Success Metrics

Không nên chỉ đo số flashcard được tạo.

Nên theo dõi:

```text
Daily Active Learners

Study Sessions per User

Average Study Time

Review Completion Rate

Quiz Accuracy

Retention Rate

Cards Mastered

Weak Topic Improvement
```

Metric quan trọng nhất:

> **Người dùng nhớ được bao nhiêu kiến thức sau khi học.**

---

# 47. Nguyên tắc UX

Ứng dụng phải tối giản.

Người dùng mở app phải biết ngay:

```text
What should I study now?
```

Không nên bắt người dùng thực hiện quá nhiều bước.

Ví dụ:

```text
Home
 ↓
12 cards due
 ↓
Start Review
```

chỉ 1 click.

---

# 48. Product Direction

App có thể phát triển theo hướng:

```text
Quizlet
+
Anki
+
AI Tutor
+
Personalized Learning
```

Điểm khác biệt chính:

> App không chỉ lưu nội dung học.

App phải hiểu:

```text
User đã biết gì?

User chưa biết gì?

User thường sai phần nào?

Khi nào user sắp quên?

User nên học gì tiếp theo?
```

---

# 49. Ưu tiên phát triển

Thứ tự ưu tiên:

```text
1. Study Set
2. Flashcards
3. Flashcard Progress
4. Quiz
5. Study Session
6. Spaced Repetition
7. Progress Dashboard
8. Weak Topics
9. AI
10. Document Import
11. Exam Planning
12. Social
```

Không nên bắt đầu bằng AI.

Core learning system phải hoạt động trước.

---

# 50. Kết luận

Mục tiêu của StudyFlow là xây dựng một hệ thống tự học giúp người dùng:

```text
Learn Faster
Remember Longer
Review Smarter
```

Core loop:

```text
Learn
↓
Recall
↓
Test
↓
Analyze
↓
Review
↓
Master
```

Nếu core loop này hoạt động tốt, sau đó có thể mở rộng AI, document import, social learning, classroom và subscription mà không làm mất trọng tâm của sản phẩm.
