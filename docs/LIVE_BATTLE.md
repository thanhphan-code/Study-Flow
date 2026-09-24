# StudyFlow Live Battle V1

## Dùng thử

- Mở một bộ học → **Live Battle** → cấu hình → **Tạo phòng & mời bạn**.
- Người tham gia mở **Battle** trên thanh điều hướng hoặc `/join/{code}`. Đăng nhập/đăng ký xong sẽ quay lại đúng lời mời.
- Host cũng là người chơi và được tính trong số chỗ. Có thể chơi một mình để kiểm tra bộ câu hỏi.
- Chốt đáp án một lần. Server chấm và lưu kết quả; câu tiếp theo tự mở sau 8 giây khi hết lượt. Host có thể chuyển sớm sau khi mọi người trả lời, hoặc kết thúc câu để ghi nhận người chưa trả lời là bỏ lỡ.
- Cuối trận, xem thứ hạng, câu cần ôn và nguồn. **Ôn lại kiến thức** mở Smart Learn trên bộ ôn cá nhân.

## Chạy ở máy phát triển

PowerShell, tại `C:\Project\StudyFLow`:

```powershell
docker compose up -d postgres
dotnet ef database update --project src/backend/StudyFlow.Infrastructure --startup-project src/backend/StudyFlow.API
dotnet run --project src/backend/StudyFlow.API
```

Các lệnh lần lượt khởi động PostgreSQL ở cổng 5433, cập nhật schema, và mở API ở cổng 5097.

Mở một cửa sổ PowerShell khác:

```powershell
Set-Location C:\Project\StudyFLow\src\frontend
npm install
npm run dev
```

Frontend chạy tại `http://localhost:5173`; Vite chuyển tiếp HTTP và WebSocket `/api` đến backend.

QR chỉ chứa URL của origin hiện tại. Khi quét bằng điện thoại thật, dùng địa chỉ LAN hoặc tên miền mà điện thoại truy cập được; `localhost` trên điện thoại không trỏ về máy tính. Ví dụ chạy Vite với `npm run dev -- --host 0.0.0.0`, rồi mở địa chỉ LAN của máy tính trước khi tạo/chia sẻ phòng. Chỉ mở cổng phát triển trên mạng tin cậy.

## Cấu trúc

```text
src/backend/
  StudyFlow.Domain/Entities/Battle.cs
  StudyFlow.Application/Battles/BattleModels.cs
  StudyFlow.Infrastructure/Persistence/
    Configurations/BattleConfiguration.cs
    Services/BattleService.cs
    Services/BattleQuestionFactory.cs
    Migrations/*LiveBattleV1*
    Migrations/*BattleQuizSources*
  StudyFlow.API/
    Battles/BattleHub.cs
    Controllers/BattlesController.cs
src/frontend/
  src/features/battles/
    battleApi.ts
    useBattleRoom.ts
    CreateBattlePage.tsx
    JoinBattlePage.tsx
    BattleRoomPage.tsx
    BattleQuestionCard.tsx
    battle.css
  e2e/battle.spec.ts
  playwright.config.ts
tests/StudyFlow.Tests/Battles/BattleApiTests.cs
```

Các điểm nối hiện có: router, AppShell, Study Set detail, chuyển hướng auth, HTTP refresh dùng chung, dependency injection, DbContext và `StudyMode.Battle`.

## Quyết định triển khai

- PostgreSQL giữ trạng thái bền vững. Mọi thay đổi phòng lấy advisory lock trong transaction; constraint bảo vệ `(room, user)` và `(question, user)`. Join code dùng RNG mật mã và unique toàn bộ lịch sử để link cũ không trỏ nhầm sang phòng mới.
- SignalR `/api/battle-hub` chỉ gửi tín hiệu `RoomChanged`, không gửi đáp án hay dữ liệu riêng của từng người. Client tải lại DTO có kiểm tra quyền; tự reconnect, đăng ký lại group và đồng bộ dự phòng mỗi 3 giây. Phòng kết thúc ngừng polling.
- Worker kiểm tra thời gian mỗi giây. Tính điểm, combo, thứ hạng và thời gian bằng đồng hồ server. Phòng hết hạn sau 6 giờ; host tối đa 5 phòng đang hoạt động.
- Mọi người nhận cùng bộ câu hỏi và lựa chọn đã snapshot. Dữ liệu nguồn bị sửa/xóa sau đó không thay đổi câu trong trận.
- Dùng quiz đã lưu (bao gồm quiz AI đã được duyệt/lưu) và flashcard của bộ học hoặc bộ nguồn đã kết hợp. Không gọi AI hay bịa nội dung mới khi tạo trận.
- Ghép cặp lấy khái niệm–ý nghĩa từ thẻ và dùng thao tác chạm. Sắp xếp chỉ dùng đáp án có ít nhất 3 bước đánh số `1.`, `2.`, `3.` hoặc `1)`, `2)`, `3)`. Thiếu dữ liệu hợp lệ thì trả lỗi rõ ràng.
- Đúng/Sai tối đa 25%. Chọn riêng Đúng/Sai không đủ để tạo trận. Dạng câu có thể chọn theo Classic/Mixed. Độ khó hiện ước lượng theo mức truy hồi của dạng câu vì nội dung cũ chưa có metadata độ khó: nhận biết → dễ, điền → vừa, tự nhớ/ghép/sắp xếp → khó. Preset ưu tiên mức phù hợp trong các dạng đã chọn, không đánh giá chuyên môn của từng nội dung.
- Trả lời ngắn dùng `IAnswerEvaluator` hiện có: đáp án chấp nhận, chuẩn hóa và gần đúng; không phải chấm ngữ nghĩa bằng LLM. Ghép/sắp xếp dùng `EvaluateSequence` trong cùng evaluator. Gần đúng nhận phản hồi riêng nhưng không nhận điểm đúng.
- Mặc định không hỏi confidence và không có hint. Confidence 0 được scheduler hiểu là không thu thập, không phải thiếu tự tin.
- Chỉ sau khi trả lời/hết giờ mới tạo thẻ snapshot trong bộ ôn riêng của người chơi. Sau đó gọi `ILearningEngine` để ghi `LearningAttempt`, `FlashcardProgress` và lịch ôn. Không mở quyền truy cập bộ học/tài liệu gốc của host cho thành viên. Việc lặp trận tạo bộ ôn mới, không gộp trí nhớ giữa các bản snapshot.
- Nguồn tài liệu được snapshot và chỉ hiện trong phản hồi sau trả lời hoặc kết quả. Không gửi đáp án/giải thích/nguồn của câu chưa trả lời xuống client.
- Điểm đúng 100, độ khó 0/15/30, combo 10/20/30, tốc độ tối đa 20. Sai/bỏ lỡ 0. Không giới hạn thời gian thì không có điểm tốc độ. Hòa điểm ưu tiên chính xác, số câu khó đúng, rồi thời gian trung bình.

## Kiểm thử

PowerShell ở thư mục gốc, dừng API đang chạy trước khi build/test trên Windows để tránh khóa DLL:

```powershell
dotnet test StudyFlow.sln
```

PowerShell ở `src/frontend`:

```powershell
npm run lint
npm run build
npx playwright install chromium
npm run test:e2e
```

E2E cần API và Vite đang chạy, migration đã áp dụng. Test tạo tài khoản/bộ học có nhãn QA trên database đang cấu hình; nên chạy bằng database phát triển riêng. E2E bao phủ hai tài khoản, đăng ký quay lại link mời, SignalR, reload/reconnect, một trận đủ 10 câu, bộ ôn cá nhân, các kiểu trả lời, bàn phím, viewport 320px và join đồng thời trên PostgreSQL. Ảnh chụp nằm trong `src/frontend/test-results/`.

Nếu triển khai nhiều API instance, PostgreSQL lock vẫn bảo vệ dữ liệu. Để phát SignalR giữa các instance ngay lập tức, cần cấu hình Redis backplane hoặc Azure SignalR; hiện client vẫn đồng bộ qua polling dự phòng. Production cần cấu hình origin CORS, HTTPS và JWT secret phù hợp với môi trường.
