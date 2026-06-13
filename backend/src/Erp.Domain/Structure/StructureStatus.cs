namespace Erp.Domain.Structure;

/// <summary>Status for structural entities (Identity spec §6). Archive instead of delete.</summary>
public enum StructureStatus
{
    Active = 0,
    Inactive = 1,
    Archived = 2,
}
