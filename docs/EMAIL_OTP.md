# Xác minh email bằng OTP

Luồng đăng ký mới không đăng nhập ngay. StudyFlow tạo mã OTP 6 chữ số, gửi tới email đăng ký và chỉ phát access/refresh token sau khi mã đúng.

## Cấu hình Gmail SMTP

Tài khoản Gmail gửi thư phải bật xác minh 2 bước và tạo **App Password** dành riêng cho StudyFlow. Không dùng mật khẩu Gmail thường và không ghi App Password vào `appsettings*.json`.

Tại `C:\Project\StudyFLow`, lưu cấu hình bằng .NET User Secrets (PowerShell):

```powershell
dotnet user-secrets set "Email:Username" "your-sender@gmail.com" --project src/backend/StudyFlow.API
dotnet user-secrets set "Email:AppPassword" "your-16-character-app-password" --project src/backend/StudyFlow.API
```

Production nên đặt secret qua secret manager hoặc biến môi trường:

```text
Email__Username=your-sender@gmail.com
Email__AppPassword=your-app-password
```

Các giá trị không nhạy cảm đã có mặc định:

```json
{
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "FromName": "StudyFlow",
    "RequireVerification": true
  }
}
```

Nếu thiếu Username hoặc AppPassword, đăng ký sẽ không giả vờ thành công và API ghi lỗi cấu hình. Tài khoản chờ vẫn được giữ để có thể gửi lại OTP sau khi cấu hình SMTP.

## Quy tắc bảo mật

- OTP gồm 6 chữ số, sinh bằng bộ sinh số mật mã và chỉ lưu HMAC-SHA-256 gắn với secret SMTP, nên không thể dò mã chỉ từ dữ liệu database.
- Mã hết hạn sau 10 phút, mã mới vô hiệu hóa mã cũ.
- Tối đa 5 lần nhập sai; cần gửi mã mới để mở khóa lượt thử.
- Gửi lại cách nhau ít nhất 60 giây.
- Endpoint đăng ký/xác minh/gửi lại giới hạn 30 request mỗi phút theo IP.
- Gửi lại với email không tồn tại vẫn trả phản hồi chung để tránh dò tài khoản.
- Login và refresh token từ chối tài khoản chưa xác minh.
- Migration đánh dấu tài khoản tồn tại trước khi triển khai là đã xác minh để không khóa người dùng cũ.

## API

```text
POST /api/auth/register          -> 202, email + thời gian OTP
POST /api/auth/verify-email      -> 200, access token + refresh cookie
POST /api/auth/resend-email-otp  -> 202
POST /api/auth/login             -> 409 EMAIL_NOT_VERIFIED nếu chưa xác minh
```

UI đăng ký tự chuyển sang màn OTP, hỗ trợ nhập từng ô, dán đủ mã, đếm ngược gửi lại và quay lại. Khi đăng nhập tài khoản chưa xác minh, UI cũng chuyển tới màn OTP.
