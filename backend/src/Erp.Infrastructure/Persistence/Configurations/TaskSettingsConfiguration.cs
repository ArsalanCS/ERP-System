using Erp.Domain.Events;
using Erp.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

public sealed class TaskSettingsConfiguration : IEntityTypeConfiguration<TaskSettings>
{
    public void Configure(EntityTypeBuilder<TaskSettings> builder)
    {
        builder.ToTable("task_settings", "bpm");
        builder.HasKey(x => x.Id);
        // One settings row per workspace.
        builder.HasIndex(x => x.WorkspaceId).IsUnique().HasFilter("is_deleted = false");
    }
}
