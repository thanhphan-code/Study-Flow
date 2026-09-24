using FluentValidation;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Time;

namespace StudyFlow.Application.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator(IUserCalendar calendar)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.DisplayName).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.");
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100).Must(calendar.IsValidTimeZone).WithMessage("TimeZoneId must be a valid IANA or system time zone identifier.");
    }
}
