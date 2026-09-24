# StudyFlow Social

Triển khai nền tảng Social V1 và giao tiếp Social V2 từ `StudyFlow_Social_Network_Feature.md`. Các nhóm học, lớp học, chat nhóm và thuật toán gợi ý nâng cao thuộc V3, chưa triển khai.

## Cách sử dụng

1. Mở **Khám phá** (`/explore`) để tìm bộ học hoặc người học. Các nguồn nội dung gồm Dành cho bạn, Đang theo dõi và Bạn bè; có bộ lọc chủ đề và sắp xếp.
2. Chọn biểu tượng hồ sơ ở góc trên → **Chỉnh sửa hồ sơ** để đặt tên người dùng, ảnh HTTPS, giới thiệu và quyền xem hồ sơ. Tài khoản cũ được cấp tên tạm duy nhất `u_…` khi migration chạy.
3. Trong bộ học của mình, chọn **Chia sẻ → Quyền chia sẻ**. Mặc định mọi bộ học vẫn riêng tư. Có bốn mức: Chỉ mình tôi, Bạn bè, Cộng đồng, Người có liên kết. Bình luận và tạo bản sao bật/tắt độc lập.
4. Trên bộ học cộng đồng, chọn **Học ngay** để trả lời thẻ và nhận đánh giá từ AnswerEvaluator. Chọn **Tạo bản sao** để có bộ học riêng, chỉnh sửa và sử dụng SmartLearn/lịch ôn cá nhân. Lượt học trực tiếp từ cộng đồng không tự thay đổi Memory Engine hoặc lịch ôn riêng.
5. Theo dõi để xem nội dung tác giả; kết bạn cần người nhận chấp thuận. Bạn bè có thể nhắn tin, gửi bộ học/thẻ và mời Battle.
6. Từ hồ sơ bạn bè, chọn **Thách đấu**, chọn bộ học, cấu hình Battle và **Tạo phòng & gửi lời mời**. Chủ phòng cũng có nút **Mời bạn bè** trong sảnh.
7. **Bộ học đã lưu** là bookmark của bản gốc. Khi mất quyền xem, chỉ hiện thông báo nội dung không còn khả dụng và nút Bỏ lưu. **Tạo bản sao** tạo nội dung độc lập và giữ thông tin tác giả gốc.
8. Các mục Tài liệu và Ôn tập đến hạn nằm trong hồ sơ, vẫn truy cập được qua URL cũ. Thanh điều hướng chính có Home, Khám phá, Môn học, Battle, Tiến độ.

## Cấu trúc thay đổi

```text
src/backend/
  StudyFlow.Domain/Entities/Social.cs
  StudyFlow.Application/Social/SocialModels.cs
  StudyFlow.Infrastructure/Persistence/
    Configurations/SocialConfiguration.cs
    Services/SocialService.cs
    Services/SocialService.Content.cs
    Services/SocialService.Messaging.cs
    Migrations/20260923060325_SocialNetwork.cs
  StudyFlow.API/
    Controllers/SocialController.cs
    Social/SocialHub.cs
src/frontend/src/features/social/
  ExplorePage.tsx
  ProfilePage.tsx
  CommunitySetPage.tsx
  PublicPractice.tsx
  MessagesPage.tsx
  ModerationPage.tsx
  ChallengeDialog.tsx
  SocialComponents.tsx
  SocialHeader.tsx
  socialApi.ts
  social.css
tests/StudyFlow.Tests/Social/SocialApiTests.cs
src/frontend/e2e/social.spec.ts
```

Các điểm tích hợp: AppShell/router, nút chia sẻ trong StudySetDetailPage, CreateBattlePage/BattleRoomPage, AuthService, User, DI, DbContext, xử lý lỗi API và Program. Query cache được xóa khi đổi/đăng xuất tài khoản. Battle chỉ trả thông tin nguồn riêng cho chủ phòng.

## Quyền riêng tư và dữ liệu

- API Social nằm dưới `/api/social`, yêu cầu JWT. Các API học cá nhân giữ nguyên điều kiện chủ sở hữu.
- `StudySetPublication` tách khỏi StudySet. Không có publication tương đương Private. Unlisted không xuất hiện trong feed hoặc tìm kiếm; chủ sở hữu có thể xem nội dung riêng của mình.
- Quan hệ Follow, Request, Friend, Block dùng cạnh có kiểu trong `social_relationships`. Friendship lưu hai chiều, chấp nhận và chặn đều thực hiện trong cùng giao dịch. Unique index chặn bản ghi trùng; lời mời bị từ chối/hủy có thời gian chờ 24 giờ trước khi gửi lại.
- Chặn có hiệu lực hai chiều: nội dung, bình luận, follow, kết bạn, tin nhắn, lời mời và truy cập phòng Battle của người bị chặn. Các cạnh follow/friend/request hiện tại được gỡ.
- Public DTO chỉ có nội dung thẻ và cờ `hasPrivateSource`; không có tên tài liệu, document/chunk ID, trang, trích đoạn, đường dẫn file, dữ liệu thành thạo hay lịch sử học. API ảnh xác thực lại quyền và dùng `Cache-Control: no-store`.
- Bản sao có thẻ và ảnh độc lập, không sao chép SourceReference/tài liệu. Attribution giữ tên bộ học và tác giả ngay cả khi bản gốc bị xóa hoặc chuyển riêng tư; chỉ trả liên kết gốc khi còn quyền truy cập.
- Chia sẻ qua chat kiểm tra quyền của cả người gửi và người nhận. Gửi bộ học Private không tự cấp quyền; người dùng cần chủ động đổi sang FriendsOnly/Public/Unlisted. Mỗi lần đọc tin nhắn, quyền xem nội dung chia sẻ được kiểm tra lại.
- Conversation có cặp thành viên duy nhất. Chỉ bạn bè hiện tại được đọc/gửi. Message, share, read receipt và thông báo không dựa vào ID do client tự khai để xác định danh tính.
- Bình luận giới hạn 2.000 ký tự, một cấp phản hồi, sửa/xóa của chính mình và xóa mềm. Tin nhắn tối đa 4.000 ký tự. Render nội dung bằng text React, không dùng HTML tùy ý.
- Các danh sách dùng trang 20 phần tử, tối đa trang 10.000. Một bộ học cộng đồng có tối đa 500 thẻ; giao diện chỉ mở dần nội dung.
- Người học duy nhất chỉ được tính khi gửi một câu trả lời hợp lệ tới endpoint học, không tính lượt mở trang. Trending dùng trọng số like 1, save 2, learner 3 trong cửa sổ 14 ngày; đây là xếp hạng đơn giản, không phải mô hình cá nhân hóa.
- Bộ giới hạn theo user: 180 lượt đọc/phút và 60 thao tác ghi/phút. Like/save và thông báo tương tác được chống trùng. Không tự thông báo cho chính mình.

## Realtime và giao dịch

SignalR `/api/social-hub` chỉ gửi sự kiện không chứa nội dung `SocialChanged` vào nhóm của tài khoản lấy từ JWT. Client tải lại DTO qua REST có kiểm tra quyền, tự reconnect, coalesce sự kiện và có polling dự phòng. Query token chỉ được chấp nhận trên hai đường dẫn hub; kết nối đóng khi token hết hạn.

Các thay đổi Social dùng transaction PostgreSQL và advisory lock chung để tránh race giữa revoke quyền, chặn, chia sẻ, kết bạn và tương tác. Semaphore trong process hỗ trợ InMemory tests. Đây là phương án đơn giản cho triển khai ban đầu; khi lưu lượng tăng cần tách lock theo cặp user/nội dung và dùng outbox/queue cho fan-out thông báo thay vì tuần tự toàn bộ mutation. Nhiều API instance cần SignalR backplane (hoặc Azure SignalR); polling vẫn là đường dự phòng.

## Quản trị

Trang `/community/moderation` hỗ trợ xem báo cáo, bỏ qua, ẩn bộ học/bình luận, tạm ngưng và khôi phục tài khoản. Chỉ ID trong cấu hình máy chủ `Social:AdminUserIds` được phép truy cập; client không thể tự gán quyền. Mặc định danh sách rỗng, không ai là admin.

Ví dụ cấu hình môi trường trước khi chạy API (PowerShell):

```powershell
$env:Social__AdminUserIds__0 = '<GUID tài khoản quản trị thực tế>'
dotnet run --project src/backend/StudyFlow.API
```

Tạm ngưng được kiểm tra với mọi request đã đăng nhập, kể cả token đã phát hành. Ẩn nội dung chặn truy cập cộng đồng và ngăn tác giả tự xuất bản lại. Tài liệu và bộ học riêng vẫn thuộc chủ sở hữu.

## Chạy và kiểm thử

Tại `C:\Project\StudyFLow`, dùng PowerShell:

```powershell
docker compose up -d postgres
dotnet ef database update --project src/backend/StudyFlow.Infrastructure --startup-project src/backend/StudyFlow.API
dotnet run --project src/backend/StudyFlow.API
```

Migration thêm bảng/index và tạo hồ sơ cho tài khoản cũ, không xuất bản bộ học cũ. Trước khi áp dụng môi trường production cần quy trình backup/migration của hệ thống.

Trong terminal PowerShell khác:

```powershell
cd C:\Project\StudyFLow\src\frontend
npm run dev -- --host 127.0.0.1
```

Kiểm tra backend tại thư mục gốc, frontend tại `src/frontend`:

```powershell
dotnet test tests/StudyFlow.Tests
```

```powershell
npm run lint
npm run build
npm run test:e2e
```

Backend tests kiểm tra IDOR, Private/FriendsOnly/Unlisted, save/revoke, remix/attribution, nguồn riêng tư, likes đồng thời, friendship/block, message membership/recipient access, reply depth, pagination, username, số người học, notification dedupe và moderation/suspension. E2E Social dùng tài khoản QA độc lập, kiểm tra giao diện 320px, học thử, chat/reconnect, thu hồi quyền xem và thách đấu qua tin nhắn. E2E Battle kiểm tra các luồng Battle hiện có.

Các phần nâng cao chưa bật: helpful-vote, huy hiệu chất lượng/AI, công khai streak/lịch sử Battle/trạng thái online, và mở nhắn tin cho người lạ. Các cờ riêng tư tương ứng được lưu ở backend nhưng thông tin hoạt động vẫn được giữ kín. Avatar hiện nhận URL HTTPS; không thêm hệ thống upload avatar riêng.
