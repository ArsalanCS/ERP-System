namespace Erp.Application.Abstractions;

/// <summary>
/// Creates a brand-new workspace together with its owner user and the standard
/// "Workspace Owner" role (self-service signup, Identity spec §6.4). Lives in
/// Infrastructure because it spans the tenant registry + identity seeding; the
/// Application layer drives it through this boundary.
/// </summary>
public interface IWorkspaceProvisioner
{
    Task<WorkspaceProvisionResult> ProvisionAsync(WorkspaceProvisionRequest request, CancellationToken cancellationToken = default);
}

public sealed record WorkspaceProvisionRequest(
    string WorkspaceName,
    string Slug,
    string DefaultLanguage,
    string TimeZone,
    string BaseCurrency,
    string Email,
    string FirstName,
    string LastName,
    string PasswordHash,
    bool ActivateImmediately);

public sealed record WorkspaceProvisionResult(Guid WorkspaceId, Guid UserId);
