using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Auditing;
using Erp.Domain.Identity;
using Erp.Shared.Errors;
using Erp.Shared.Results;

namespace Erp.Application.Users;

/// <summary>
/// Users management (Identity spec §4). All operations are workspace-scoped via
/// the tenant context; writes are audited; suspend/archive revoke sessions; the
/// last active Workspace Owner is protected (§4.6).
/// </summary>
public sealed class UserService(
    IUserRepository users,
    IRoleRepository roles,
    IEmployeeRepository employees,
    IStructureRepository structure,
    IRefreshTokenRepository refreshTokens,
    IPasswordResetTokenRepository resetTokens,
    ITokenHasher tokenHasher,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IAuditLogger audit,
    IClock clock,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : IUserService
{
    private const string OwnerRoleCode = "workspace-owner";

    public async Task<PagedResult<UserListItem>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var (items, total) = await users.ListAsync(query.Search, query.Status, query.Sort, query.Page, query.PageSize, cancellationToken);
        var emps = await employees.GetByUserIdsAsync(items.Select(u => u.Id).ToList(), cancellationToken);
        var mapped = items.Select(u =>
        {
            var emp = emps.GetValueOrDefault(u.Id);
            return new UserListItem(u.Id, u.Email, u.DisplayName, emp?.Mobile, emp?.JobTitle,
                u.Status, u.TwoFactorEnabled, u.LastLoginAt, u.CreatedAt);
        }).ToList();
        return new PagedResult<UserListItem>(mapped, query.Page, query.PageSize, total);
    }

    public async Task<Result<UserDetail>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return UserErrors.NotFound();
        var roleIds = await users.GetRoleIdsAsync(id, cancellationToken);
        var emp = await employees.GetByUserIdAsync(id, cancellationToken);
        return Map(user, emp, roleIds);
    }

    public async Task<Result<CreateUserResult>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId) return Result.Failure<CreateUserResult>(UserErrors.NoWorkspaceScope());

        if (await users.EmailExistsAsync(workspaceId, request.Email, cancellationToken))
        {
            return UserErrors.EmailTaken();
        }

        if (await ValidateRolesAsync(request.RoleIds, cancellationToken) is { } roleError)
        {
            return Result.Failure<CreateUserResult>(roleError);
        }

        if (request.PlacementNodeId is { } placeId && !await structure.NodeExistsAsync(placeId, cancellationToken))
        {
            return Result.Failure<CreateUserResult>(UserErrors.UnknownPlacementNode());
        }

        var user = new User(workspaceId, request.Email, request.FirstName, request.LastName);
        user.UpdateProfile(request.FirstName, request.LastName, $"{request.FirstName} {request.LastName}".Trim(),
            request.PreferredLanguage, request.TimeZone ?? "Asia/Riyadh");
        users.Add(user);

        // Employee (HR) record — 1:1 with the user, holds details + tree placement.
        var employee = new Employee(workspaceId, user.Id);
        employee.UpdateDetails(request.EmployeeNumber, request.JobTitle, request.Mobile, request.HireDate);
        employee.PlaceAt(request.PlacementNodeId);
        employee.SetManager(request.ManagerId);
        employees.Add(employee);

        if (request.RoleIds is { Count: > 0 })
        {
            await roles.SetUserRolesAsync(workspaceId, user.Id, request.RoleIds, cancellationToken);
        }

        string? invitationToken = null;
        if (request.SendInvitation)
        {
            var secret = tokenGenerator.NewSecret();
            invitationToken = $"{workspaceId:N}.{secret}";
            resetTokens.Add(new PasswordResetToken(workspaceId, user.Id, tokenHasher.Hash(invitationToken),
                clock.UtcNow.AddDays(7)));
        }

        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Create, Module = "Identity", ResourceType = "User",
            ResourceId = user.Id.ToString(), WorkspaceId = workspaceId,
            NewValues = $"{{\"email\":\"{user.Email}\",\"status\":\"{user.Status}\"}}",
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Deliver the invitation (dev: written to the local outbox + logged).
        if (invitationToken is not null)
        {
            await emailSender.SendInvitationAsync(user.Email, user.DisplayName, invitationToken, cancellationToken);
        }

        // The raw token travels by email only — never returned to the API caller.
        return new CreateUserResult(user.Id, null);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId) return Result.Failure(UserErrors.NoWorkspaceScope());

        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound());

        if (await ValidateRolesAsync(request.RoleIds, cancellationToken) is { } roleError)
        {
            return Result.Failure(roleError);
        }

        if (request.PlacementNodeId is { } placeId && !await structure.NodeExistsAsync(placeId, cancellationToken))
        {
            return Result.Failure(UserErrors.UnknownPlacementNode());
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.DisplayName,
            request.PreferredLanguage, request.TimeZone);
        user.SetAccessWindow(request.AccessStartDate, request.AccessExpiryDate);

        // Employee details — create the record lazily if it doesn't exist yet.
        var employee = await employees.GetByUserIdAsync(user.Id, cancellationToken);
        if (employee is null)
        {
            employee = new Employee(workspaceId, user.Id);
            employees.Add(employee);
        }
        employee.UpdateDetails(request.EmployeeNumber, request.JobTitle, request.Mobile, request.HireDate);
        employee.PlaceAt(request.PlacementNodeId);
        employee.SetManager(request.ManagerId);

        if (request.RoleIds is not null)
        {
            await roles.SetUserRolesAsync(workspaceId, user.Id, request.RoleIds, cancellationToken);
        }

        await audit.LogAsync(Entry(AuditActions.Update, user, workspaceId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SuspendAsync(Guid id, SuspendUserRequest request, CancellationToken cancellationToken = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId) return Result.Failure(UserErrors.NoWorkspaceScope());

        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound());

        if (await WouldRemoveLastOwnerAsync(user.Id, cancellationToken)) return Result.Failure(UserErrors.LastWorkspaceOwner());

        user.Suspend(); // rotates security stamp → revokes access tokens
        await refreshTokens.RevokeAllForUserAsync(user.Id, clock.UtcNow, cancellationToken);

        await audit.LogAsync(Entry(AuditActions.Update, user, workspaceId, request.Reason), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ReactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId) return Result.Failure(UserErrors.NoWorkspaceScope());

        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound());

        user.Reactivate();
        await audit.LogAsync(Entry(AuditActions.Update, user, workspaceId, "Reactivated"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId) return Result.Failure(UserErrors.NoWorkspaceScope());

        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return Result.Failure(UserErrors.NotFound());

        if (await WouldRemoveLastOwnerAsync(user.Id, cancellationToken)) return Result.Failure(UserErrors.LastWorkspaceOwner());

        user.Archive(tenant.WorkspaceId is null ? null : user.Id, clock.UtcNow);
        await refreshTokens.RevokeAllForUserAsync(user.Id, clock.UtcNow, cancellationToken);

        await audit.LogAsync(Entry(AuditActions.Delete, user, workspaceId, "Archived"), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Error?> ValidateRolesAsync(IReadOnlyList<Guid>? roleIds, CancellationToken cancellationToken)
    {
        if (roleIds is not { Count: > 0 }) return null;
        var existing = await roles.FilterExistingIdsAsync(roleIds, cancellationToken);
        return existing.Count == roleIds.Distinct().Count() ? null : UserErrors.UnknownRole();
    }

    private async Task<bool> WouldRemoveLastOwnerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var ownerRole = (await roles.ListAsync(cancellationToken)).FirstOrDefault(r => r.Code == OwnerRoleCode);
        if (ownerRole is null) return false;

        var userRoleIds = await users.GetRoleIdsAsync(userId, cancellationToken);
        if (!userRoleIds.Contains(ownerRole.Id)) return false;

        var activeOwners = await users.CountActiveByRoleAsync(ownerRole.Id, cancellationToken);
        return activeOwners <= 1;
    }

    private static AuditEntry Entry(string action, User user, Guid workspaceId, string? reason = null) => new()
    {
        Action = action, Module = "Identity", ResourceType = "User",
        ResourceId = user.Id.ToString(), WorkspaceId = workspaceId, Reason = reason,
        NewValues = $"{{\"status\":\"{user.Status}\"}}",
    };

    private static UserDetail Map(User u, Employee? e, IReadOnlyList<Guid> roleIds) => new(
        u.Id, u.WorkspaceId, u.Email, u.FirstName, u.LastName, u.DisplayName, e?.Mobile,
        u.PreferredLanguage, u.TimeZone, e?.JobTitle, e?.EmployeeNumber, e?.PlacementNodeId, e?.ManagerId, e?.HireDate,
        u.Status, u.TwoFactorEnabled, u.RequirePasswordChange,
        u.AccessStartDate, u.AccessExpiryDate, u.LastLoginAt, u.CreatedAt, roleIds);
}
