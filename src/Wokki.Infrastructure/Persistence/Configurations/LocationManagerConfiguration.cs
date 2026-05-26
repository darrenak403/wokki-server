using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class LocationManagerConfiguration : IEntityTypeConfiguration<LocationManager>
{
    public void Configure(EntityTypeBuilder<LocationManager> builder)
    {
        builder.ToTable("location_managers");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.LocationId, x.UserId })
            .IsUnique()
            .HasDatabaseName("IX_location_managers_location_user_unique");

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedBy)
            .WithMany()
            .HasForeignKey(x => x.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
