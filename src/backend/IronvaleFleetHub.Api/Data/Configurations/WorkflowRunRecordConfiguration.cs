using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Data.Configurations;

public class WorkflowRunRecordConfiguration : IEntityTypeConfiguration<WorkflowRunRecord>
{
    public void Configure(EntityTypeBuilder<WorkflowRunRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkflowName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(4000);
        builder.HasIndex(x => x.WorkflowName);
        builder.HasIndex(x => x.StartedAtUtc);
    }
}
