using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using StudyFlow.Application.Auth.Interfaces;

namespace StudyFlow.Infrastructure.Authentication;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string AppPassword { get; set; } = "";
    public string FromName { get; set; } = "StudyFlow";
    public bool RequireVerification { get; set; } = true;
}

internal sealed class SmtpEmailVerificationSender(IOptions<EmailOptions> options) : IEmailVerificationSender
{
    private readonly EmailOptions settings = options.Value;
    public async Task SendOtpAsync(string recipient, string displayName, string code, int expiresInMinutes, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.AppPassword))
            throw new InvalidOperationException("Email OTP is enabled but Email:Username or Email:AppPassword is not configured.");
        using var message = new MailMessage
        {
            From = new MailAddress(settings.Username, settings.FromName),
            Subject = $"{code} là mã xác minh StudyFlow",
            IsBodyHtml = true,
            Body = $"""
                <!doctype html><html lang="vi"><body style="margin:0;background:#f3f7fc;font-family:Arial,sans-serif;color:#20314b">
                <div style="max-width:520px;margin:32px auto;background:#fff;border:1px solid #dfe7f1;border-radius:20px;padding:32px">
                <div style="font-size:24px;font-weight:800;color:#1f4fc1">StudyFlow</div>
                <h1 style="font-size:24px;margin:28px 0 10px">Xác minh email của bạn</h1>
                <p style="line-height:1.7;color:#64748b">Chào {WebUtility.HtmlEncode(displayName)}, nhập mã dưới đây để hoàn tất đăng ký.</p>
                <div style="margin:28px 0;padding:20px;text-align:center;background:#edf3ff;border-radius:14px;font-size:34px;font-weight:800;letter-spacing:10px;color:#245dc1">{code}</div>
                <p style="line-height:1.7;color:#64748b">Mã có hiệu lực trong {expiresInMinutes} phút. Không chia sẻ mã này với bất kỳ ai.</p>
                <p style="font-size:12px;color:#94a3b8">Nếu bạn không tạo tài khoản StudyFlow, hãy bỏ qua email này.</p>
                </div></body></html>
                """
        };
        message.To.Add(recipient);
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = true, UseDefaultCredentials = false, Credentials = new NetworkCredential(settings.Username, settings.AppPassword) };
        await client.SendMailAsync(message, cancellationToken);
    }
}
