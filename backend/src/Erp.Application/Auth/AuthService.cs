using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Domain.Identity;
using Erp.Shared.Results;
using Microsoft.Extensions.Options;

namespace Erp.Application.Auth;

/// <summary>
/// Auth core (Identity spec §11, CLAUDE.md §4.5): login with lockout, refresh
/// with rotation + theft detection, logout, forgot/reset password. Per the
/// per-workspace identity model, every flow is scoped to a workspace resolved
/// from the slug (login/forgot) or embedded in the opaque token (refresh/reset).
/// </summary>
public sealed class AuthService(
    IWorkspaceRepository workspaces,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordResetTokenRepository resetTokens,
    IPasswordHasher passwordHasher,
    ITokenHasher tokenHasher,
    ITokenGenerator tokenGenerator,
    ITotpService totp,
    IEmailSender emailSender,
    IJwtTokenService jwt,
    IPermissionResolver permissionResolver,
    IAuditLogger audit,
    IClock clock,
    ITenantContext tenant,
    IUnitOfWork unitOfWork,
    IOptions<AuthOptions> options) : IAuthService
{
    private readonly AuthOptions _options = options.Value;

    public async Task<Result<AuthTokens>> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var workspace = await workspaces.GetBySlugAsync(request.WorkspaceSlug, cancellationToken);
        if (workspace is null || !workspace.AllowsLogin)
        {
            return AuthErrors.InvalidCredentials();
        }

        using var _ = tenant.BeginScope(workspace.Id, []);

        var user = await users.GetByEmailAsync(workspace.Id, request.Email, cancellationToken);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials();
        }

        if (user.IsLockedOut(now))
        {
            return AuthErrors.AccountLocked();
        }

        var verification = passwordHasher.Verify(user.PasswordHash ?? string.Empty, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RegisterFailedLogin(_options.MaxFailedAccessAttempts, TimeSpan.FromMinutes(_options.LockoutMinutes), now);
            await audit.LogAsync(new AuditEntry
            {
                Action = AuditActions.FailedLogin,
                Module = "Identity",
                ResourceType = "User",
                ResourceId = user.Id.ToString(),
                Result = AuditResult.Failed,
                WorkspaceId = workspace.Id,
                ActorUserId = user.Id,
                ActorDisplayName = user.Email,
                Reason = "Invalid password",
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthErrors.InvalidCredentials();
        }

        if (!user.CanLogin(now))
        {
            return AuthErrors.AccountNotActive();
        }

        // Second factor (TOTP authenticator) when the account has it enabled (spec §7).
        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
            {
                return AuthErrors.TwoFactorRequired();
            }
            if (string.IsNullOrEmpty(user.TwoFactorSecret)
                || !totp.VerifyCode(user.TwoFactorSecret, request.TwoFactorCode))
            {
                await audit.LogAsync(new AuditEntry
                {
                    Action = AuditActions.FailedLogin, Module = "Identity", ResourceType = "User",
                    ResourceId = user.Id.ToString(), Result = AuditResult.Failed, WorkspaceId = workspace.Id,
                    ActorUserId = user.Id, ActorDisplayName = user.Email, Reason = "Invalid 2FA code",
                }, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return AuthErrors.InvalidTwoFactorCode();
            }
        }

        user.RegisterSuccessfulLogin(now);
        var (tokens, _) = await IssueTokensAsync(workspace.Id, user, ip, now, cancellationToken);
        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Login,
            Module = "Identity",
            ResourceType = "User",
            ResourceId = user.Id.ToString(),
            Result = AuditResult.Success,
            WorkspaceId = workspace.Id,
            ActorUserId = user.Id,
            ActorDisplayName = user.Email,
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    public async Task<Result<AuthTokens>> RefreshAsync(RefreshRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        if (!TryGetWorkspace(request.RefreshToken, out var workspaceId))
        {
            return AuthErrors.InvalidRefreshToken();
        }

        using var _ = tenant.BeginScope(workspaceId, []);

        var hash = tokenHasher.Hash(request.RefreshToken);
        var existing = await refreshTokens.GetByHashAsync(hash, cancellationToken);
        if (existing is null)
        {
            return AuthErrors.InvalidRefreshToken();
        }

        if (!existing.IsActive(now))
        {
            // Re-use of an already-rotated token = theft. Revoke the whole chain.
            if (existing.RevokedAt is not null)
            {
                await refreshTokens.RevokeAllForUserAsync(existing.UserId, now, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return AuthErrors.InvalidRefreshToken();
        }

        var user = await users.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || !user.CanLogin(now))
        {
            return AuthErrors.InvalidRefreshToken();
        }

        var (tokens, replacement) = await IssueTokensAsync(workspaceId, user, ip, now, cancellationToken);
        existing.RotateTo(replacement.Id, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    public async Task<Result> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        if (!TryGetWorkspace(request.RefreshToken, out var workspaceId))
        {
            return Result.Success(); // idempotent
        }

        using var _ = tenant.BeginScope(workspaceId, []);

        var hash = tokenHasher.Hash(request.RefreshToken);
        var existing = await refreshTokens.GetByHashAsync(hash, cancellationToken);
        if (existing is not null)
        {
            existing.Revoke(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var workspace = await workspaces.GetBySlugAsync(request.WorkspaceSlug, cancellationToken);
        if (workspace is null)
        {
            return new ForgotPasswordResult(null);
        }

        using var _ = tenant.BeginScope(workspace.Id, []);

        var user = await users.GetByEmailAsync(workspace.Id, request.Email, cancellationToken);
        if (user is null || user.Status is UserStatus.Archived)
        {
            return new ForgotPasswordResult(null);
        }

        var secret = tokenGenerator.NewSecret();
        var raw = ComposeToken(workspace.Id, secret);
        var token = new PasswordResetToken(workspace.Id, user.Id, tokenHasher.Hash(raw),
            now.AddHours(_options.PasswordResetTokenHours));
        resetTokens.Add(token);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Deliver the reset link (dev: written to the local outbox + logged).
        await emailSender.SendPasswordResetAsync(user.Email, user.DisplayName, raw, cancellationToken);

        // The raw token travels by email only — never returned to the requester.
        return new ForgotPasswordResult(raw);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        if (!TryGetWorkspace(request.Token, out var workspaceId))
        {
            return Result.Failure(AuthErrors.InvalidResetToken());
        }

        using var _ = tenant.BeginScope(workspaceId, []);

        var hash = tokenHasher.Hash(request.Token);
        var token = await resetTokens.GetByHashAsync(hash, cancellationToken);
        if (token is null || !token.IsUsable(now))
        {
            return Result.Failure(AuthErrors.InvalidResetToken());
        }

        var user = await users.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidResetToken());
        }

        user.SetPasswordHash(passwordHasher.Hash(request.NewPassword)); // rotates security stamp
        if (user.Status == UserStatus.PendingInvitation)
        {
            user.Activate();
        }

        token.Consume(now);
        // Old sessions are revoked on reset (Identity spec §11).
        await refreshTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken);
        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.PasswordChange,
            Module = "Identity",
            ResourceType = "User",
            ResourceId = user.Id.ToString(),
            Result = AuditResult.Success,
            WorkspaceId = workspaceId,
            ActorUserId = user.Id,
            ActorDisplayName = user.Email,
            Reason = "Password reset",
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // ---- helpers -----------------------------------------------------------

    private async Task<(AuthTokens Tokens, RefreshToken RefreshToken)> IssueTokensAsync(
        Guid workspaceId, User user, string? ip, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Resolve effective actions (deny-wins applied) for the access-token claims.
        var permissions = await permissionResolver.ResolveAsync(user.Id, cancellationToken);
        var access = jwt.CreateAccessToken(user, permissions.Actions, clusterIds: [], isPlatformAdmin: false);

        var secret = tokenGenerator.NewSecret();
        var raw = ComposeToken(workspaceId, secret);
        var refreshToken = new RefreshToken(workspaceId, user.Id, tokenHasher.Hash(raw),
            now.AddDays(_options.RefreshTokenDays), ip);
        refreshTokens.Add(refreshToken);

        var tokens = new AuthTokens(
            access.Token,
            access.ExpiresAt,
            raw,
            refreshToken.ExpiresAt,
            new AuthUser(user.Id, workspaceId, user.Email, user.DisplayName, user.PreferredLanguage,
                user.RequirePasswordChange, user.TwoFactorEnabled));

        return (tokens, refreshToken);
    }

    private static string ComposeToken(Guid workspaceId, string secret) => $"{workspaceId:N}.{secret}";

    private static bool TryGetWorkspace(string token, out Guid workspaceId)
    {
        workspaceId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token)) return false;
        var dot = token.IndexOf('.');
        if (dot <= 0) return false;
        return Guid.TryParseExact(token[..dot], "N", out workspaceId);
    }
}
