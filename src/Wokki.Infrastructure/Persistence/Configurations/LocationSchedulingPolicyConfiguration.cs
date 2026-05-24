using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class LocationSchedulingPolicyConfiguration : IEntityTypeConfiguration<LocationSchedulingPolicy>
{
    public void Configure(EntityTypeBuilder<LocationSchedulingPolicy> builder)
    {
        builder.ToTable("location_scheduling_policies");
        builder.HasKey(x => x.LocationId);
        builder.Property(x => x.RulesJson).HasColumnType("jsonb").IsRequired();

        builder.HasOne<Location>()
            .WithOne()
            .HasForeignKey<LocationSchedulingPolicy>(x => x.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
