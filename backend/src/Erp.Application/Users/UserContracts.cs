using Erp.Application.Common;
using Erp.Domain.Identity;

namespace Erp.Application.Users;

public sealed record UserListItem(
    Guid Id,
    string Email,
    string DisplayName,
    string? Mobile,
    string? JobTitle,
    UserStatus Status,
    bool TwoFactorEnabled,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);

public sealed record UserDetail(
    Guid Id,
    Guid WorkspaceId,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Mobile,
    string PreferredLanguage,
    string TimeZone,
    string? JobTitle,
    string? EmployeeNumber,
    Guid? PlacementNodeId,
    Guid? ManagerId,
    DateTimeOffset? HireDate,
    UserStatus Status,
    bool TwoFactorEnabled,
    bool RequirePasswordChange,
    DateTimeOffset? AccessStartDate,
    DateTimeOffset? AccessExpiryDate,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> RoleIds);

/// <summary>List query with user-specific filters.</summary>
public sealed record UserListQuery : ListQuery
{
    public UserStatus? Status { get; init; }
}

public sealed record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string? Mobile,
    string? JobTitle,
    string PreferredLanguage,
    string? TimeZone,
    string? EmployeeNumber,
    Guid? PlacementNodeId,
    Guid? ManagerId,
    DateTimeOffset? HireDate,
    IReadOnlyList<Guid>? RoleIds,
    bool SendInvitation);

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string DisplayName,
    string? Mobile,
    string? JobTitle,
    string PreferredLanguage,
    string TimeZone,
    string? EmployeeNumber,
    Guid? PlacementNodeId,
    Guid? ManagerId,
    DateTimeOffset? HireDate,
    DateTimeOffset? AccessStartDate,
    DateTimeOffset? AccessExpiryDate,
    IReadOnlyList<Guid>? RoleIds);

public sealed record SuspendUserRequest(string Reason);

/// <summary>Result of creating/inviting a user; the invite token is for the mailer only.</summary>
public sealed record CreateUserResult(Guid UserId, string? InvitationToken);
