using Erp.Domain.Common;

namespace Erp.Domain.Assets;

/// <summary>
/// Global lookup describing the kind of <see cref="Asset"/> (Event/Asset architecture §10):
/// NOTE, DOCUMENT, CUSTOMER, etc. Code-driven; seeded once at startup. Only NOTE and
/// DOCUMENT are used this phase — the rest are placeholders for future modules.
/// </summary>
public sealed class AssetType : BaseEntity
{
    private AssetType() { } // EF

    public AssetType(string code, string name)
    {
        Code = code.Trim();
        Name = name.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
}

/// <summary>Stable asset-type codes. NOTE and DOCUMENT are active this phase.</summary>
public static class AssetTypeCodes
{
    public const string Note = "NOTE";
    public const string Document = "DOCUMENT";
    public const string Customer = "CUSTOMER";
    public const string Supplier = "SUPPLIER";
    public const string Vehicle = "VEHICLE";
    public const string Invoice = "INVOICE";
    public const string Resource = "RESOURCE";
}
