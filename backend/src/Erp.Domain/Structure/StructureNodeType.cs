namespace Erp.Domain.Structure;

/// <summary>
/// The kind of node in the business-structure tree (Identity spec §6). The tree
/// is a single self-nesting hierarchy: Organization at the root, then any of the
/// finer-grained types beneath it (organization → department → branch →
/// sub-department → team → sub-team), with members (employees) placed on nodes.
/// </summary>
public enum StructureNodeType
{
    Organization = 0,
    Department = 1,
    Branch = 2,
    SubDepartment = 3,
    Team = 4,
    SubTeam = 5,
}
