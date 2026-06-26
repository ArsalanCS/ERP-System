namespace Erp.Domain.Tasks;

/// <summary>
/// Platform event classification. In this phase Task is the ONLY event type
/// (Task Model spec §2): notes, documents, customers, invoices, etc. are linked
/// supporting records, never events. Kept as an enum so the engine can expand later.
/// </summary>
public enum TaskEventType
{
    Task = 1,
}

/// <summary>Task priority (serialized as numbers — keep in sync with the frontend mirror).</summary>
public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3,
}

/// <summary>
/// Reporting category a workspace status maps onto (Task Model spec §4). Lets
/// custom per-workspace statuses roll up to a common, comparable lifecycle.
/// </summary>
public enum TaskStatusCategory
{
    Open = 0,
    InProgress = 1,
    Waiting = 2,
    Review = 3,
    Completed = 4,
    Cancelled = 5,
    Rejected = 6,
}

/// <summary>Kinds of user-visible task activity (the "Logs" tab; Task Model spec §5).</summary>
public enum TaskActivityKind
{
    Created = 0,
    Updated = 1,
    Assigned = 2,
    StatusChanged = 3,
    Scheduled = 4,
    Archived = 5,
    SubtaskAdded = 6,
    NoteAdded = 7,
    DocumentAdded = 8,
    RelationChanged = 9,
}

/// <summary>The role a linked record plays for a task (Task Model spec §8).</summary>
public enum TaskRelationRole
{
    PrimarySource = 0,
    Supporting = 1,
    Reference = 2,
}

/// <summary>Dependency type between two tasks (Task Model spec §7-8; classic scheduling links).</summary>
public enum TaskDependencyType
{
    FinishToStart = 0,
    StartToStart = 1,
    FinishToFinish = 2,
    StartToFinish = 3,
}
