using FluentValidation;
using StudyFlow.Application.Auth.DTOs;
using StudyFlow.Application.Time;

namespace StudyFlow.Application.Auth.Validators;

public sealed class UpdateTimeZoneRequestValidator : AbstractValidator<UpdateTimeZoneRequest>
{
    public UpdateTimeZoneRequestValidator(IUserCalendar calendar)
    {
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100).Must(calendar.IsValidTimeZone).WithMessage("TimeZoneId must be a valid IANA or system time zone identifier.");
    }
}
