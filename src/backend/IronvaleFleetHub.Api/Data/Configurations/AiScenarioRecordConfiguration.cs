using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Data.Configurations;

public class AiScenarioRecordConfiguration : IEntityTypeConfiguration<AiScenarioRecord>
{
    public void Configure(EntityTypeBuilder<AiScenarioRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ScenarioType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PredictedIssue).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
        builder.Property(x => x.RecommendedAction).HasMaxLength(1000);
        builder.Property(x => x.Priority).HasMaxLength(20);
        builder.Property(x => x.Explanation).HasMaxLength(2000);
        builder.HasIndex(x => x.EquipmentId);
    }
}
