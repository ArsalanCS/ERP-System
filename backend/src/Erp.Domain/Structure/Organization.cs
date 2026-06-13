using Erp.Domain.Common;

namespace Erp.Domain.Structure;

/// <summary>A legal entity / company inside a workspace (Identity spec §6.5).</summary>
public sealed class Organization : TenantEntity
{
    private Organization() { } // EF

    public Organization(Guid workspaceId, string name, string code)
    {
        AssignWorkspace(workspaceId);
        Name = name;
        Code = code;
        Status = StructureStatus.Active;
    }

    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string? LegalName { get; private set; }
    public string? OrganizationType { get; private set; }
    public string? CommercialRegistrationNumber { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string BaseCurrency { get; private set; } = "SAR";
    public Guid? ResponsibleManagerId { get; private set; }
    public StructureStatus Status { get; private set; }

    public void Update(string name, string? legalName, string? type, string? crn, string? taxNumber,
        string? country, string? city, string baseCurrency, Guid? managerId)
    {
        Name = name;
        LegalName = legalName;
        OrganizationType = type;
        CommercialRegistrationNumber = crn;
        TaxNumber = taxNumber;
        Country = country;
        City = city;
        BaseCurrency = baseCurrency;
        ResponsibleManagerId = managerId;
    }

    public void Archive(Guid? by, DateTimeOffset when)
    {
        Status = StructureStatus.Archived;
        SoftDelete(by, when);
    }
}
