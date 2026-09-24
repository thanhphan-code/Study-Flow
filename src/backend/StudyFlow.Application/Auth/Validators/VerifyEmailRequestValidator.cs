using FluentValidation;
using StudyFlow.Application.Auth.DTOs;

namespace StudyFlow.Application.Auth.Validators;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$").WithMessage("OTP must contain exactly 6 digits.");
    }
}
public sealed class ResendEmailOtpRequestValidator : AbstractValidator<ResendEmailOtpRequest>
{
    public ResendEmailOtpRequestValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
}
