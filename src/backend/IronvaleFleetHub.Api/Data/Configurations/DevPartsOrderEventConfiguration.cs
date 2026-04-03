using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IronvaleFleetHub.Api.Models;

namespace IronvaleFleetHub.Api.Data.Configurations;

public class DevPartsOrderEventConfiguration : IEntityTypeConfiguration<DevPartsOrderEvent>
{
    public void Configure(EntityTypeBuilder<DevPartsOrderEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalEventId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NewStatus).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TrackingNumber).HasMaxLength(200);
        builder.HasIndex(x => x.ExternalEventId).IsUnique();
        builder.HasIndex(x => x.PartsOrderId);
    }
}
