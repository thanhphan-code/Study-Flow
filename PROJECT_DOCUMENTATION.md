# Tài liệu dự án StudyFlow

## 1. Tổng quan

**StudyFlow** là nền tảng hỗ trợ tự học bằng flashcard, quiz, lặp lại ngắt quãng (spaced repetition), học thích ứng và theo dõi tiến độ. Người dùng có thể tổ chức kiến thức theo môn học và bộ học, nhập tài liệu, tạo nội dung thủ công hoặc nhờ AI hỗ trợ, sau đó luyện tập qua nhiều chế độ học khác nhau.

Ứng dụng hiện được xây dựng dưới dạng web app gồm:

- Frontend React phục vụ giao diện người dùng.
- Backend ASP.NET Core cung cấp REST API.
- PostgreSQL lưu dữ liệu lâu dài.
- Gemini hỗ trợ sinh nội dung dựa trên tài liệu nguồn.
- Local file storage lưu tài liệu và ảnh flashcard ngoài public web root.

## 2. Mục tiêu sản phẩm

StudyFlow hướng tới một quy trình học khép kín:

1. Tạo môn học và bộ học.
2. Thêm flashcard thủ công, nhập nhanh, tải tài liệu hoặc dùng AI tạo bản nháp.
3. Học bằng flashcard, quiz, Smart Learn hoặc kế hoạch Daily Study.
4. Ghi nhận kết quả và tự động lên lịch ôn tập.
5. Theo dõi tiến độ, lịch sử, chuỗi ngày học và các nội dung còn yếu.

## 3. Công nghệ sử dụng

### Frontend

- React 19 và TypeScript.
- Vite 8 để phát triển và build.
- React Router 7 để định tuyến.
- TanStack React Query để quản lý server state và cache.
- Zustand để quản lý trạng thái phía client.
- Tailwind CSS 4 cho giao diện.
- Web Speech API của trình duyệt để phát âm nội dung flashcard.

### Backend

- ASP.NET Core trên .NET 10.
- Entity Framework Core 10.
- PostgreSQL 17 và Npgsql.
- JWT access token và refresh token.
- FluentValidation cho dữ liệu đầu vào.
- PdfPig cùng bộ trích xuất nội dung tài liệu.
- Gemini API cho các chức năng AI.

### Hạ tầng và kiểm thử

- Docker Compose để chạy PostgreSQL trong môi trường phát triển.
- xUnit và ASP.NET Core Test Host cho integration test.
- EF Core Migrations để quản lý schema.

## 4. Kiến trúc dự án

Backend tuân theo kiến trúc phân lớp với chiều phụ thuộc:

```text
API -> Infrastructure -> Application -> Domain
```

```text
StudyFlow/
├── src/
│   ├── backend/
│   │   ├── StudyFlow.API/            # Controller, middleware, cấu hình ứng dụng
│   │   ├── StudyFlow.Application/    # DTO, interface, validator và nghiệp vụ dùng chung
│   │   ├── StudyFlow.Domain/         # Entity, enum và quy tắc miền
│   │   └── StudyFlow.Infrastructure/ # EF Core, PostgreSQL, dịch vụ, AI và lưu file
│   └── frontend/
│       └── src/
│           ├── api/                  # HTTP client dùng chung
│           ├── components/           # Thành phần giao diện cấp ứng dụng
│           ├── features/             # Module theo từng tính năng
│           ├── i18n/                 # Ngôn ngữ giao diện
│           ├── pages/                # Trang dùng chung
│           └── routes/               # Cấu hình route
└── tests/
    └── StudyFlow.Tests/              # Unit test và integration test
```

Các module frontend được tổ chức theo tính năng như `auth`, `subjects`, `studySets`, `flashcards`, `quizzes`, `smartLearning`, `dailyStudy`, `documents`, `dashboard`, `progress` và `insights`.

### Learning Engine thống nhất

Mọi kết quả học từ Flashcard Study, Due Review, Quiz có liên kết flashcard, Smart Learn, Daily Study và Study Together đều đi qua `ILearningEngine`. Engine ghi `LearningAttempt`, cập nhật `FlashcardProgress`, lịch ôn, mastery, weakness và study session trong cùng pipeline. `ILearningStatePolicy` cung cấp một định nghĩa chung cho thẻ mới, đến hạn, yếu, độ ưu tiên và hành động tiếp theo; các mode chỉ bổ sung constraint và flow riêng.

Quyết định kiến trúc và các trade-off được ghi tại [`docs/adr/001-unified-learning-engine.md`](docs/adr/001-unified-learning-engine.md).

Memory Engine V2 phân biệt chất lượng bằng chứng giữa nhận diện, tự nhớ, ứng dụng, câu trả lời có gợi ý, độ tự tin và thời gian phản hồi. Mỗi lần cập nhật SRS tạo một `LearningDecision` để lưu trạng thái trước/sau cùng phiên bản scheduler và policy. Chi tiết tại [`docs/adr/002-memory-engine-v2.md`](docs/adr/002-memory-engine-v2.md).

## 5. Chức năng hiện có

### 5.1. Tài khoản và xác thực

- Đăng ký tài khoản.
- Đăng nhập và lấy thông tin người dùng hiện tại.
- Cấp JWT access token có thời hạn ngắn.
- Làm mới phiên bằng refresh token luân chuyển lưu trong cookie `HttpOnly`.
- Đăng xuất và thu hồi refresh token.
- Bảo vệ route frontend và API cần đăng nhập.
- Phân tách dữ liệu theo chủ sở hữu; người dùng chỉ truy cập được tài nguyên của mình.
- Lưu múi giờ người dùng; đăng ký mới tự gửi múi giờ trình duyệt và API cho phép cập nhật về sau.

### 5.2. Quản lý môn học

- Xem danh sách môn học.
- Tạo, xem chi tiết, cập nhật và xóa môn học.
- Hiển thị các bộ học thuộc từng môn.
- Không cho xóa môn học khi vẫn còn bộ học liên quan.

### 5.3. Quản lý bộ học

- Tạo, xem, sửa và xóa bộ học.
- Tổ chức bộ học bên trong một môn học.
- Tạo **bộ học kết hợp** từ nhiều bộ nguồn.
- Cập nhật danh sách nguồn của bộ học kết hợp.
- Học chung nội dung từ nhiều bộ bằng chức năng **Study Together**.
- Quan hệ sở hữu được suy ra qua `StudySet -> Subject -> User`, tránh lưu trùng `UserId`.

### 5.4. Flashcard

- Tạo, sửa, xóa và xem danh sách flashcard.
- Tạo hàng loạt flashcard trong một giao dịch nguyên tử.
- Nhập nhanh nhiều thẻ từ văn bản.
- Giữ thứ tự ổn định trong bộ học.
- Hỗ trợ câu hỏi, câu trả lời và phần giải thích.
- Hỗ trợ ngôn ngữ BCP 47, cách đọc và phiên âm/romanization.
- Hỗ trợ ví dụ theo ngữ cảnh, bản dịch ví dụ, mẹo ghi nhớ và nhiều đáp án được chấp nhận.
- Gắn một ảnh JPG, PNG hoặc WEBP cho mỗi thẻ, tối đa 5 MB.
- Thay thế, tải/xem và xóa ảnh qua API có kiểm tra quyền sở hữu.
- Xóa flashcard hoặc bộ học sẽ dọn file ảnh liên quan.
- Phát âm bằng Web Speech API với tốc độ thường, chậm và chế độ tự phát tùy chọn.

### 5.5. Học flashcard và ôn tập ngắt quãng

- Trình học flashcard theo từng bộ.
- Tiết lộ lần lượt nội dung gợi nhớ, cách đọc, nghĩa và nút đánh giá.
- Đánh giá kết quả theo bốn mức: `Again`, `Hard`, `Good`, `Easy`.
- Lưu một bản ghi tiến độ cho mỗi cặp người dùng–flashcard.
- Tự động tính trạng thái và lịch ôn tiếp theo.
- Truy vấn hàng đợi các thẻ đã đến hạn.
- Theo dõi các trạng thái `New`, `Learning`, `Reviewing`, `Mastered`.

### 5.6. Quiz

- Sinh quiz từ các flashcard đang có.
- Tạo quiz thủ công.
- Hỗ trợ câu hỏi trắc nghiệm và đúng/sai.
- Bắt đầu một lượt làm bài, lưu hoặc thay đổi đáp án trước khi nộp.
- Không tiết lộ đáp án đúng khi bài chưa hoàn thành.
- Chấm điểm phía server khi nộp bài.
- Hiển thị lại các câu trả lời sai sau khi hoàn thành.
- Lượt làm đã hoàn thành là bất biến.
- Tự động ghi nhận study session cho quiz hoàn chỉnh.

### 5.7. Smart Learn

- Tạo và khôi phục phiên Smart Learn đang hoạt động của một bộ học.
- Ưu tiên thẻ đến hạn, thẻ yếu, thẻ mới và thẻ vừa trả lời sai.
- Có thể bắt đầu nội dung mới bằng nhận diện trước khi chuyển sang tự nhớ.
- Hỗ trợ các dạng bài: recall, multiple choice, true/false, application và similar question.
- Ghi nhận kết quả `Correct`, `Close` hoặc `Wrong`.
- Lưu độ tự tin, thời gian phản hồi, việc dùng gợi ý và trạng thái lật đáp án.
- Đưa thẻ trả lời sai quay lại sau một số câu xen kẽ.
- Lưu trạng thái từng thẻ trong phiên và kết quả cuối phiên.
- Có gợi ý AI hai cấp khi bộ học có tài liệu nguồn phù hợp.

### 5.8. Daily Study

- Tạo kế hoạch học xuyên suốt thư viện theo thời lượng 5, 10, 15 phút hoặc không giới hạn.
- Cân bằng thẻ đến hạn, thẻ yếu và thẻ mới.
- Hỗ trợ tự nhập câu trả lời và so khớp có dung sai lỗi gõ.
- Luân phiên các kiểu luyện với thẻ ngoại ngữ: nghe, nhớ ngược và nhớ nghĩa.
- Hiển thị ví dụ, bản dịch, mẹo ghi nhớ và đáp án được chấp nhận sau khi trả lời.

### 5.9. Phiên học

- Bắt đầu và hoàn tất study session.
- Ghi nhận chế độ học, thời lượng và khối lượng hoạt động.
- Hỗ trợ các chế độ `Flashcard`, `Review`, `Quiz`, `Learn`, `DailyStudy` và `StudyTogether`.
- Chỉ phiên đã hoàn thành và có hoạt động mới được đưa vào thống kê.

### 5.10. Dashboard và phân tích tiến độ

- Dashboard tổng hợp số thẻ đến hạn.
- Thống kê thời gian học trong ngày theo múi giờ riêng của người dùng.
- Thống kê số thẻ đã ôn, số câu quiz và độ chính xác tổng hợp.
- Hiển thị năm bộ học gần đây.
- Tổng quan tiến độ toàn thư viện và theo từng bộ học.
- Phân bố flashcard theo trạng thái.
- Tính độ chính xác quiz có trọng số.
- Phát hiện thẻ yếu dựa trên lịch sử đúng/sai, ease factor, trạng thái và khoảng cách ôn.
- Đề xuất số lượng thẻ cần ôn.
- Theo dõi chuỗi ngày học hiện tại, chuỗi dài nhất và tổng số ngày hoạt động.
- Biểu đồ lịch sử học liên tục trong 7–365 ngày, bao gồm cả ngày không có hoạt động.

### 5.11. Quản lý tài liệu

- Tải lên PDF, DOCX, PPTX và TXT UTF-8.
- Giới hạn kích thước file 20 MB.
- Kiểm tra phần mở rộng, MIME type và chữ ký file.
- Trích xuất văn bản đồng bộ và lưu trạng thái xử lý.
- Xem danh sách, metadata, nội dung preview và lỗi xử lý.
- Liên kết tài liệu với môn học hoặc bộ học thuộc người dùng.
- Gắn tài liệu có sẵn vào bộ học.
- Xóa đồng thời metadata và file vật lý.
- File được lưu ngoài public web root bằng storage key sinh tự động; database không lưu binary gốc.

### 5.12. AI với Gemini

- Sinh bản nháp flashcard từ tài liệu nguồn.
- Sinh bản nháp quiz từ tài liệu nguồn.
- Giải thích flashcard dựa trên ngữ cảnh tài liệu.
- Sinh câu hỏi tương tự.
- Cung cấp gợi ý gia sư cho Smart Learn.
- Theo dõi vòng đời AI job: `Pending`, `Processing`, `Completed`, `Failed`.
- Kiểm tra quyền sở hữu, giới hạn đầu vào và validate output AI ở backend.
- Nội dung AI chỉ là bản nháp; chỉ được đưa vào bộ học sau khi người dùng xác nhận lưu.

### 5.13. Giao diện và ngôn ngữ

- Thiết kế mobile-first, responsive cho màn hình desktop.
- Thanh điều hướng dưới trên thiết bị nhỏ và bố cục lưới phù hợp trên desktop.
- Hỗ trợ tiếng Anh và tiếng Việt.
- Ghi nhớ ngôn ngữ đã chọn trong `localStorage` với khóa `studyflow-language`.
- Có trạng thái loading, error và empty state ở các luồng chính.

## 6. Các trang frontend

| Route | Chức năng |
|---|---|
| `/login` | Đăng nhập |
| `/register` | Đăng ký |
| `/home` | Dashboard |
| `/subjects` | Danh sách môn học |
| `/subjects/:id` | Chi tiết môn học và các bộ học |
| `/subjects/:id/combine` | Tạo bộ học kết hợp |
| `/study-sets/:id` | Quản lý chi tiết một bộ học |
| `/study/:id/flashcards` | Học flashcard |
| `/study/:id/learn` | Smart Learn |
| `/study/today` | Daily Study |
| `/study-together` | Học chung nhiều bộ |
| `/reviews/due` | Ôn các thẻ đến hạn |
| `/quizzes/:id` | Làm quiz |
| `/documents` | Danh sách và tải tài liệu |
| `/documents/:id` | Chi tiết và preview tài liệu |
| `/progress` | Tiến độ, insight và lịch sử học |

Ngoại trừ trang đăng nhập và đăng ký, các route nghiệp vụ đều được bảo vệ bởi `ProtectedRoute`.

## 7. Nhóm REST API chính

| Nhóm | Prefix/endpoint tiêu biểu | Mục đích |
|---|---|---|
| Auth | `/api/auth/*` | Đăng ký, đăng nhập, refresh, lấy user hiện tại, đăng xuất |
| Subjects | `/api/subjects/*` | CRUD môn học |
| Study Sets | `/api/study-sets/*`, `/api/subjects/{id}/study-sets` | CRUD và bộ học kết hợp |
| Study Together | `/api/study-together` | Tạo trải nghiệm học từ nhiều bộ |
| Flashcards | `/api/study-sets/{id}/flashcards`, `/api/flashcards/*` | CRUD, bulk create và ảnh thẻ |
| Reviews | `/api/flashcards/{id}/review`, `/api/reviews/due` | Ghi nhận đánh giá và lấy thẻ đến hạn |
| Quizzes | `/api/quizzes/*`, `/api/quiz-attempts/*` | Quiz, lượt làm, đáp án và chấm điểm |
| Smart Learn | `/api/study-sets/{id}/smart-learn`, `/api/smart-learn/sessions/*` | Phiên học thích ứng |
| Daily Study | `/api/daily-study` | Lập kế hoạch học trong ngày |
| Sessions | `/api/study-sessions/*` | Theo dõi phiên học |
| Dashboard | `/api/dashboard` | Số liệu tổng quan |
| Progress | `/api/progress/*`, `/api/study-sets/{id}/progress` | Insight, streak, lịch sử và thẻ yếu |
| Documents | `/api/documents/*`, `/api/study-sets/{id}/documents` | Upload, preview, xóa và gắn tài liệu |
| AI | `/api/*/ai/*`, `/api/ai/jobs/*` | Sinh nội dung, giải thích, câu tương tự và gợi ý |
| Health | `/api/health` | Kiểm tra tình trạng API |

## 8. Mô hình dữ liệu chính

- `User`: tài khoản và thông tin xác thực.
- `Subject`: môn học do người dùng sở hữu.
- `StudySet`: bộ học chuẩn hoặc bộ học kết hợp.
- `StudySetSource`: liên kết bộ học kết hợp với các bộ nguồn.
- `Flashcard`: nội dung học, dữ liệu ngôn ngữ, ví dụ và metadata ảnh.
- `FlashcardProgress`: trạng thái ghi nhớ và lịch ôn của từng người dùng.
- `Quiz`, `Question`, `AnswerOption`: cấu trúc quiz.
- `QuizAttempt`, `QuizAnswer`: lượt làm và câu trả lời.
- `StudySession`: hoạt động học đã ghi nhận.
- `Document`: metadata, trạng thái và văn bản trích xuất từ tài liệu.
- `AIJob`: yêu cầu AI, trạng thái và kết quả sinh.
- `LearningAttempt`: từng lần trả lời trong Smart Learn.
- `SmartLearningCard`: trạng thái của thẻ trong một phiên Smart Learn.

## 9. Bảo mật và tính toàn vẹn dữ liệu

- JWT được kiểm tra issuer, audience, thời hạn và chữ ký.
- Refresh token được luân chuyển và lưu trong cookie `HttpOnly`.
- API nghiệp vụ kiểm tra người dùng hiện tại và quyền sở hữu tài nguyên.
- Request được validate trước khi xử lý.
- Lỗi API được chuẩn hóa qua exception handler.
- File upload được kiểm tra tên mở rộng, MIME, chữ ký và kích thước.
- File người dùng không được public trực tiếp; việc truy cập đi qua endpoint có xác thực.
- AI output được backend validate trước khi cho phép lưu.
- Quan hệ và constraint trong PostgreSQL giúp bảo vệ tính toàn vẹn dữ liệu.
- CORS môi trường phát triển chỉ cho phép frontend tại `http://localhost:5173`.

> Các khóa JWT và credential database trong cấu hình development chỉ dành cho máy phát triển. Môi trường production phải dùng biến môi trường, secret manager hoặc .NET User Secrets và không commit secret vào Git.

## 10. Cách chạy dự án

### Yêu cầu

- .NET SDK 10.
- Node.js và npm tương thích với Vite 8.
- Docker Desktop.

### Khởi động database và backend

Chạy trong **PowerShell** tại thư mục gốc của repository:

```powershell
docker compose up -d postgres
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/backend/StudyFlow.Infrastructure --startup-project src/backend/StudyFlow.API
dotnet run --project src/backend/StudyFlow.API
```

Backend mặc định chạy tại `http://localhost:5097`. PostgreSQL development được ánh xạ ra cổng `5433`.

### Khởi động frontend

Mở một cửa sổ **PowerShell** khác:

```powershell
Set-Location src/frontend
npm install
npm run dev
```

Frontend mặc định chạy tại `http://localhost:5173` và proxy các request `/api` sang backend.

### Cấu hình Gemini

Chạy tại thư mục gốc và thay giá trị mẫu bằng API key thật:

```powershell
dotnet user-secrets init --project src/backend/StudyFlow.API
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY" --project src/backend/StudyFlow.API
```

Không đưa Gemini API key vào `appsettings.json` hoặc commit lên repository.

## 11. Kiểm tra chất lượng

Chạy backend test tại thư mục gốc:

```powershell
dotnet test StudyFlow.sln
```

Kiểm tra frontend:

```powershell
Set-Location src/frontend
npm run lint
npm run build
```

Bộ test hiện bao phủ các nhóm chính như kiến trúc phân lớp, auth, subject, study set, flashcard, progress, quiz, study session, dashboard, lịch sử học, tài liệu, AI, Smart Learn và Daily Study.

## 12. Giới hạn và lưu ý hiện tại

- Người dùng cũ được mặc định ở UTC cho đến khi cập nhật múi giờ; giao diện cài đặt múi giờ riêng chưa được bổ sung.
- Chất lượng và danh sách giọng đọc phụ thuộc trình duyệt và hệ điều hành vì phát âm dùng Web Speech API.
- Các chức năng Gemini cần API key hợp lệ và kết nối mạng.
- Xử lý/trích xuất tài liệu hiện diễn ra đồng bộ.
- File hiện lưu trên ổ đĩa cục bộ; triển khai nhiều instance cần chuyển sang shared/object storage.
- Cấu hình development không phù hợp để dùng trực tiếp trong production.

## 13. Trạng thái hiện tại

Ứng dụng đã có đầy đủ luồng cốt lõi từ quản lý nội dung, luyện tập, đánh giá kết quả đến phân tích tiến độ. Codebase đã có migration, validation, kiểm tra quyền sở hữu, integration test và cấu trúc module đủ rõ để tiếp tục mở rộng các tính năng như thông báo nhắc học, xử lý tài liệu nền, object storage và triển khai production.
