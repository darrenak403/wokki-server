using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class PlatformActivityEventConfiguration : IEntityTypeConfiguration<PlatformActivityEvent>
{
    public void Configure(EntityTypeBuilder<PlatformActivityEvent> builder)
    {
        builder.ToTable("platform_activity_events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(80);

        builder.HasIndex(x => new { x.OrganizationId, x.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.EventType, x.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.OrganizationId, x.EventType, x.OccurredAt }).IsDescending(false, false, true);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
