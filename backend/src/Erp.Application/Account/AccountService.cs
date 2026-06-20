using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Shared.Errors;
using Erp.Shared.Results;

namespace Erp.Application.Account;

public sealed record MyProfileDto(Guid Id, string Email, string FirstName, string LastName, string DisplayName,
    string? Mobile, string PreferredLanguage, string TimeZone, string? JobTitle, bool TwoFactorEnabled);

public sealed record UpdateMyProfileRequest(string FirstName, string LastName, string DisplayName,
    string? Mobile, string PreferredLanguage, string TimeZone);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record SessionDto(Guid Id, string? CreatedByIp, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

public sealed record TwoFactorSetupDto(string Secret, string OtpAuthUri);

public sealed record TwoFactorCodeRequest(string Code);

public interface IAccountService
{
    Task<Result<MyProfileDto>> GetProfileAsync(CancellationToken ct = default);
    Task<Result> UpdateProfileAsync(UpdateMyProfileRequest request, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SessionDto>>> ListSessionsAsync(CancellationToken ct = default);
    Task<Result> RevokeSessionAsync(Guid sessionId, CancellationToken ct = default);

    // ---- Two-factor (TOTP authenticator app, Identity spec §7) -------------
    Task<Result<TwoFactorSetupDto>> SetupTwoFactorAsync(CancellationToken ct = default);
    Task<Result> EnableTwoFactorAsync(TwoFactorCodeRequest request, CancellationToken ct = default);
    Task<Result> DisableTwoFactorAsync(TwoFactorCodeRequest request, CancellationToken ct = default);
}

/// <summary>Personal account (Identity spec §10). Self-service for the current user.</summary>
public sealed class AccountService(
    ICurrentUser currentUser,
    IUserRepository users,
    IEmployeeRepository employees,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITotpService totp,
    IAuditLogger audit,
    IClock clock,
    IUnitOfWork unitOfWork) : IAccountService
{
    private const string TwoFactorIssuer = "Xonfo ERP";

    public async Task<Result<MyProfileDto>> GetProfileAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure<MyProfileDto>(AccountErrors.Unauthenticated());
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure<MyProfileDto>(AccountErrors.Unauthenticated());
        var employee = await employees.GetByUserIdAsync(userId, ct);
        return new MyProfileDto(user.Id, user.Email, user.FirstName, user.LastName, user.DisplayName,
            employee?.Mobile, user.PreferredLanguage, user.TimeZone, employee?.JobTitle, user.TwoFactorEnabled);
    }

    public async Task<Result> UpdateProfileAsync(UpdateMyProfileRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure(AccountErrors.Unauthenticated());
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure(AccountErrors.Unauthenticated());

        user.UpdateProfile(request.FirstName, request.LastName, request.DisplayName,
            request.PreferredLanguage, request.TimeZone);

        // Mobile lives on the employee record now — create it lazily, preserve job title.
        var employee = await employees.GetByUserIdAsync(userId, ct);
        if (employee is null)
        {
            employee = new Domain.Identity.Employee(user.WorkspaceId, user.Id);
            employees.Add(employee);
        }
        employee.SetContact(employee.JobTitle, request.Mobile);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure(AccountErrors.Unauthenticated());
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure(AccountErrors.Unauthenticated());

        if (passwordHasher.Verify(user.PasswordHash ?? string.Empty, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return Result.Failure(AccountErrors.WrongCurrentPassword());
        }

        user.SetPasswordHash(passwordHasher.Hash(request.NewPassword)); // rotates security stamp
        await refreshTokens.RevokeAllForUserAsync(user.Id, clock.UtcNow, ct); // revoke other sessions
        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.PasswordChange, Module = "Identity", ResourceType = "User",
            ResourceId = user.Id.ToString(), WorkspaceId = user.WorkspaceId, ActorUserId = user.Id,
            Reason = "Self-service change",
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> ListSessionsAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure<IReadOnlyList<SessionDto>>(AccountErrors.Unauthenticated());
        var sessions = await refreshTokens.ListActiveAsync(userId, clock.UtcNow, ct);
        IReadOnlyList<SessionDto> result = sessions.Select(s => new SessionDto(s.Id, s.CreatedByIp, s.CreatedAt, s.ExpiresAt)).ToList();
        return Result.Success(result);
    }

    public async Task<Result> RevokeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure(AccountErrors.Unauthenticated());
        var revoked = await refreshTokens.RevokeByIdAsync(sessionId, userId, clock.UtcNow, ct);
        if (!revoked) return Result.Failure(Error.NotFound("Session not found."));
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<TwoFactorSetupDto>> SetupTwoFactorAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure<TwoFactorSetupDto>(AccountErrors.Unauthenticated());
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure<TwoFactorSetupDto>(AccountErrors.Unauthenticated());

        var secret = totp.GenerateSecret();
        user.BeginTwoFactorEnrollment(secret); // pending until confirmed; not yet enabled
        await unitOfWork.SaveChangesAsync(ct);

        var uri = totp.BuildOtpAuthUri(secret, user.Email, TwoFactorIssuer);
        return Result.Success(new TwoFactorSetupDto(secret, uri));
    }

    public async Task<Result> EnableTwoFactorAsync(TwoFactorCodeRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure(AccountErrors.Unauthenticated());
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure(AccountErrors.Unauthenticated());

        if (string.IsNullOrEmpty(user.TwoFactorSecret) || !totp.VerifyCode(user.TwoFactorSecret, request.Code))
        {
            return Result.Failure(AccountErrors.InvalidTwoFactorCode());
        }

        user.ConfirmTwoFactor(); // rotates security stamp
        await refreshTokens.RevokeAllForUserAsync(user.Id, clock.UtcNow, ct);
        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.TwoFactorEnabled, Module = "Identity", ResourceType = "User",
            ResourceId = user.Id.ToString(), WorkspaceId = user.WorkspaceId, ActorUserId = user.Id,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DisableTwoFactorAsync(TwoFactorCodeRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure(AccountErrors.Unauthenticated());
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure(AccountErrors.Unauthenticated());

        // Require a valid current code to disable (proves possession of the authenticator).
        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret)
            || !totp.VerifyCode(user.TwoFactorSecret, request.Code))
        {
            return Result.Failure(AccountErrors.InvalidTwoFactorCode());
        }

        user.DisableTwoFactor(); // rotates security stamp
        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.TwoFactorDisabled, Module = "Identity", ResourceType = "User",
            ResourceId = user.Id.ToString(), WorkspaceId = user.WorkspaceId, ActorUserId = user.Id,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal static class AccountErrors
{
    public static Error Unauthenticated() => Error.Unauthorized("Not authenticated.");
    public static Error WrongCurrentPassword() =>
        new("ACCOUNT_WRONG_PASSWORD", "The current password is incorrect.", ErrorType.Validation);
    public static Error InvalidTwoFactorCode() =>
        new("ACCOUNT_INVALID_2FA_CODE", "The verification code is invalid or has expired.", ErrorType.Validation);
}
