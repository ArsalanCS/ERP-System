using Erp.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class TaskChecklistItemConfiguration : IEntityTypeConfiguration<TaskChecklistItem>
{
    public void Configure(EntityTypeBuilder<TaskChecklistItem> builder)
    {
        builder.ToTable("task_checklist_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Text).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.WorkspaceId, x.TaskId });
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskNoteConfiguration : IEntityTypeConfiguration<TaskNote>
{
    public void Configure(EntityTypeBuilder<TaskNote> builder)
    {
        builder.ToTable("task_notes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(x => new { x.WorkspaceId, x.TaskId });
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskDocumentConfiguration : IEntityTypeConfiguration<TaskDocument>
{
    public void Configure(EntityTypeBuilder<TaskDocument> builder)
    {
        builder.ToTable("task_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FileType).HasMaxLength(60);
        builder.Property(x => x.Url).HasMaxLength(2000);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.HasIndex(x => new { x.WorkspaceId, x.TaskId });
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("task_dependencies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DependencyType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.WorkspaceId, x.TaskId });
        builder.HasIndex(x => new { x.TaskId, x.DependsOnTaskId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.DependsOnTaskId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TaskRelationConfiguration : IEntityTypeConfiguration<TaskRelation>
{
    public void Configure(EntityTypeBuilder<TaskRelation> builder)
    {
        builder.ToTable("task_relations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.HasIndex(x => new { x.WorkspaceId, x.TaskId });
        builder.HasIndex(x => new { x.TaskId, x.RelatedEntityType, x.RelatedEntityId, x.Role })
            .IsUnique().HasFilter("is_deleted = false");
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}
