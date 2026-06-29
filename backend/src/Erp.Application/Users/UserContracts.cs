using Erp.Application.Common;
using Erp.Domain.Identity;

namespace Erp.Application.Users;

public sealed record UserListItem(
    long Id,
    string Email,
    string DisplayName,
    string? Mobile,
    string? JobTitle,
    UserStatus Status,
    bool TwoFactorEnabled,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);

public sealed record UserDetail(
    long Id,
    long WorkspaceId,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Mobile,
    string PreferredLanguage,
    string TimeZone,
    string? JobTitle,
    string? EmployeeNumber,
    long? PlacementNodeId,
    long? ManagerId,
    DateTimeOffset? HireDate,
    UserStatus Status,
    bool TwoFactorEnabled,
    bool RequirePasswordChange,
    DateTimeOffset? AccessStartDate,
    DateTimeOffset? AccessExpiryDate,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<long> RoleIds);

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
    long? PlacementNodeId,
    long? ManagerId,
    DateTimeOffset? HireDate,
    IReadOnlyList<long>? RoleIds,
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
    long? PlacementNodeId,
    long? ManagerId,
    DateTimeOffset? HireDate,
    DateTimeOffset? AccessStartDate,
    DateTimeOffset? AccessExpiryDate,
    IReadOnlyList<long>? RoleIds);

public sealed record SuspendUserRequest(string Reason);

/// <summary>Result of creating/inviting a user; the invite token is for the mailer only.</summary>
public sealed record CreateUserResult(long UserId, string? InvitationToken);
