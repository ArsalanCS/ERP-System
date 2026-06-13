using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Domain.Identity;
using Erp.Shared.Errors;
using Erp.Shared.Results;
using FluentValidation;

namespace Erp.Application.Security;

public sealed record SecurityPolicyDto(
    int PasswordMinLength, bool RequireUppercase, bool RequireLowercase, bool RequireDigit,
    bool RequireSymbol, int? PasswordExpiryDays, int MaxFailedAttempts, int LockoutMinutes,
    int SessionIdleTimeoutMinutes, int RefreshTokenDays, bool RequireTwoFactor);

public sealed record UpdateSecurityPolicyRequest(
    int PasswordMinLength, bool RequireUppercase, bool RequireLowercase, bool RequireDigit,
    bool RequireSymbol, int? PasswordExpiryDays, int MaxFailedAttempts, int LockoutMinutes,
    int SessionIdleTimeoutMinutes, int RefreshTokenDays, bool RequireTwoFactor);

public interface ISecurityPolicyService
{
    Task<Result<SecurityPolicyDto>> GetAsync(CancellationToken ct = default);
    Task<Result> UpdateAsync(UpdateSecurityPolicyRequest request, CancellationToken ct = default);
}

/// <summary>Workspace security policy (Identity spec §7): get-or-create + update.</summary>
public sealed class SecurityPolicyService(
    ISecurityPolicyRepository policies,
    ITenantContext tenant,
    IAuditLogger audit,
    IUnitOfWork unitOfWork) : ISecurityPolicyService
{
    public async Task<Result<SecurityPolicyDto>> GetAsync(CancellationToken ct = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId)
        {
            return Result.Failure<SecurityPolicyDto>(Error.Unauthorized("No workspace scope."));
        }
        var policy = await GetOrCreateAsync(workspaceId, ct);
        return Map(policy);
    }

    public async Task<Result> UpdateAsync(UpdateSecurityPolicyRequest request, CancellationToken ct = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId)
        {
            return Result.Failure(Error.Unauthorized("No workspace scope."));
        }
        var policy = await GetOrCreateAsync(workspaceId, ct);

        policy.Update(
            request.PasswordMinLength, request.RequireUppercase, request.RequireLowercase, request.RequireDigit,
            request.RequireSymbol, request.PasswordExpiryDays, request.MaxFailedAttempts, request.LockoutMinutes,
            request.SessionIdleTimeoutMinutes, request.RefreshTokenDays, request.RequireTwoFactor);

        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Update, Module = "Identity", ResourceType = "SecurityPolicy",
            ResourceId = workspaceId.ToString(), WorkspaceId = workspaceId,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<WorkspaceSecurityPolicy> GetOrCreateAsync(Guid workspaceId, CancellationToken ct)
    {
        var policy = await policies.GetForWorkspaceAsync(workspaceId, ct);
        if (policy is null)
        {
            policy = new WorkspaceSecurityPolicy(workspaceId);
            policies.Add(policy);
            await unitOfWork.SaveChangesAsync(ct);
        }
        return policy;
    }

    private static SecurityPolicyDto Map(WorkspaceSecurityPolicy p) => new(
        p.PasswordMinLength, p.RequireUppercase, p.RequireLowercase, p.RequireDigit, p.RequireSymbol,
        p.PasswordExpiryDays, p.MaxFailedAttempts, p.LockoutMinutes, p.SessionIdleTimeoutMinutes,
        p.RefreshTokenDays, p.RequireTwoFactor);
}

public sealed class UpdateSecurityPolicyValidator : AbstractValidator<UpdateSecurityPolicyRequest>
{
    public UpdateSecurityPolicyValidator()
    {
        RuleFor(x => x.PasswordMinLength).InclusiveBetween(6, 128);
        RuleFor(x => x.MaxFailedAttempts).InclusiveBetween(1, 20);
        RuleFor(x => x.LockoutMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.SessionIdleTimeoutMinutes).InclusiveBetween(5, 10080);
        RuleFor(x => x.RefreshTokenDays).InclusiveBetween(1, 365);
        RuleFor(x => x.PasswordExpiryDays).InclusiveBetween(1, 3650).When(x => x.PasswordExpiryDays.HasValue);
    }
}
