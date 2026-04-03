using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Data.Configurations;

public class WorkflowDispatchRecordConfiguration : IEntityTypeConfiguration<WorkflowDispatchRecord>
{
    public void Configure(EntityTypeBuilder<WorkflowDispatchRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DispatchKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.WorkflowName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100);
        builder.HasIndex(x => x.DispatchKey).IsUnique();
        builder.HasIndex(x => x.WorkflowName);
    }
}
