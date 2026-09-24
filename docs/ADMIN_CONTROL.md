# StudyFlow Admin Control Center

## Cấp quyền admin gốc

Admin gốc được xác định bằng danh sách email trong cấu hình máy chủ. Không ghi email quản trị trực tiếp vào `appsettings.json` của repository.

```powershell
dotnet user-secrets set "Admin:BootstrapEmails:0" "admin@gmail.com" --project .\src\backend\StudyFlow.API\StudyFlow.API.csproj
```

Với production, dùng biến môi trường:

```text
Admin__BootstrapEmails__0=admin@gmail.com
```

Tài khoản phải hoàn tất xác minh email. Ở lần đăng nhập hoặc refresh token kế tiếp, máy chủ cấp vai trò `Admin` và thu hồi token cũ nếu vai trò thay đổi.

## Truy cập

- Giao diện: `/admin`
- API: `/api/admin/*`
- Khu kiểm duyệt cộng đồng: `/community/moderation`

Frontend chỉ hiển thị liên kết quản trị cho admin. Backend luôn kiểm tra role trong JWT, phiên bản phiên và trạng thái tài khoản trước khi xử lý request.

## Chức năng

- Tổng quan user đang hoạt động trong 15 phút gần nhất.
- Tìm kiếm theo email hoặc tên; lọc theo hoạt động, tạm ngưng, xác minh và vai trò.
- Xem số môn học, học phần, phiên học, tài liệu và lượt Battle của từng user.
- Tạm ngưng hoặc mở lại tài khoản với lý do bắt buộc.
- Thu hồi access token hiện tại và refresh token bằng `SessionVersion`.
- Cấp hoặc hạ quyền admin.
- Audit log bất biến ở mức API cho mọi thao tác quản trị.
- Bảo vệ khỏi tự khóa, tự hạ quyền và thay đổi admin gốc từ giao diện.

## Quy tắc vận hành

- Chỉ cấp admin cho người cần quyền vận hành.
- Dùng lý do cụ thể; lý do được lưu trong audit log.
- Ưu tiên tạm ngưng thay vì xóa user để giữ quan hệ dữ liệu và lịch sử kiểm toán.
- Khi nghi ngờ lộ tài khoản, dùng **Thu hồi tất cả phiên đăng nhập** trước khi điều tra.
- Thay đổi danh sách admin gốc chỉ qua secret store hoặc biến môi trường của máy chủ.
