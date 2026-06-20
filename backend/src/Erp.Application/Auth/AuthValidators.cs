using FluentValidation;

namespace Erp.Application.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.WorkspaceSlug).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.WorkspaceSlug).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(256)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

public sealed class RegisterWorkspaceRequestValidator : AbstractValidator<RegisterWorkspaceRequest>
{
    private static readonly string[] AllowedCurrencies = ["SAR", "AED", "USD", "EUR", "GBP", "BHD", "KWD", "QAR", "OMR"];
    private static readonly string[] AllowedLanguages = ["en", "ar"];

    public RegisterWorkspaceRequestValidator()
    {
        RuleFor(x => x.WorkspaceName).NotEmpty().MaximumLength(120);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MinimumLength(3).WithMessage("Workspace address must be at least 3 characters.")
            .MaximumLength(40)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Use lowercase letters, numbers and hyphens only.");

        RuleFor(x => x.BaseCurrency)
            .NotEmpty()
            .Must(c => AllowedCurrencies.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Unsupported base currency.");

        RuleFor(x => x.Language)
            .NotEmpty()
            .Must(l => AllowedLanguages.Contains(l, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Unsupported language.");

        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(256)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator() => RuleFor(x => x.Token).NotEmpty();
}

public sealed class ResendVerificationRequestValidator : AbstractValidator<ResendVerificationRequest>
{
    public ResendVerificationRequestValidator()
    {
        RuleFor(x => x.WorkspaceSlug).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
