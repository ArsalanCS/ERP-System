using FluentValidation;

namespace Erp.Application.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Mobile).MaximumLength(32);
        RuleFor(x => x.JobTitle).MaximumLength(150);
        RuleFor(x => x.PreferredLanguage).NotEmpty().MaximumLength(8);
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PreferredLanguage).NotEmpty().MaximumLength(8);
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.AccessExpiryDate)
            .GreaterThan(x => x.AccessStartDate)
            .When(x => x is { AccessStartDate: not null, AccessExpiryDate: not null })
            .WithMessage("Access expiry must be after the start date.");
    }
}

public sealed class SuspendUserRequestValidator : AbstractValidator<SuspendUserRequest>
{
    public SuspendUserRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}
