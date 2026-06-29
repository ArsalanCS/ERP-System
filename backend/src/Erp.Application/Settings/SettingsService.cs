using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Shared.Errors;
using Erp.Shared.Results;

namespace Erp.Application.Settings;

public sealed record WorkspaceSettingsDto(long Id, string Name, string? LegalName, string Slug,
    string DefaultLanguage, string TimeZone, string BaseCurrency, string? Country, string Status);

public sealed record UpdateWorkspaceSettingsRequest(string Name, string? LegalName,
    string DefaultLanguage, string TimeZone, string BaseCurrency, string? Country);

public interface ISettingsService
{
    Task<Result<WorkspaceSettingsDto>> GetAsync(CancellationToken ct = default);
    Task<Result> UpdateAsync(UpdateWorkspaceSettingsRequest request, CancellationToken ct = default);
}

/// <summary>Workspace settings (Identity spec §9): general + localization.</summary>
public sealed class SettingsService(
    IWorkspaceRepository workspaces,
    IAuditLogger audit,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : ISettingsService
{
    public async Task<Result<WorkspaceSettingsDto>> GetAsync(CancellationToken ct = default)
    {
        if (tenant.WorkspaceId is not { } id) return Result.Failure<WorkspaceSettingsDto>(SettingsErrors.NoScope());
        var ws = await workspaces.GetByIdAsync(id, ct);
        if (ws is null) return Result.Failure<WorkspaceSettingsDto>(Error.NotFound("Workspace not found."));
        return new WorkspaceSettingsDto(ws.Id, ws.Name, ws.LegalName, ws.Slug, ws.DefaultLanguage, ws.TimeZone,
            ws.BaseCurrency, ws.Country, ws.Status.ToString());
    }

    public async Task<Result> UpdateAsync(UpdateWorkspaceSettingsRequest request, CancellationToken ct = default)
    {
        if (tenant.WorkspaceId is not { } id) return Result.Failure(SettingsErrors.NoScope());
        var ws = await workspaces.GetByIdAsync(id, ct);
        if (ws is null) return Result.Failure(Error.NotFound("Workspace not found."));

        ws.UpdateProfile(request.Name, request.LegalName, request.DefaultLanguage, request.TimeZone, request.BaseCurrency, request.Country);
        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Update, Module = "Settings", ResourceType = "Workspace",
            ResourceId = ws.Id.ToString(), WorkspaceId = ws.Id,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal static class SettingsErrors
{
    public static Error NoScope() => new("SETTINGS_NO_WORKSPACE", "No workspace scope on the request.", ErrorType.Forbidden);
}
