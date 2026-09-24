using FluentValidation;

namespace StudyFlow.Application.Admin;

public sealed class UpdateUserStatusRequestValidator : AbstractValidator<UpdateUserStatusRequest>
{
    public UpdateUserStatusRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RevokeUserSessionsRequestValidator : AbstractValidator<RevokeUserSessionsRequest>
{
    public RevokeUserSessionsRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
