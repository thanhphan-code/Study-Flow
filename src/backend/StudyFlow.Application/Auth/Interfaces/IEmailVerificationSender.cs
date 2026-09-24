namespace StudyFlow.Application.Auth.Interfaces;

public interface IEmailVerificationSender
{
    Task SendOtpAsync(string recipient, string displayName, string code, int expiresInMinutes, CancellationToken cancellationToken);
}
