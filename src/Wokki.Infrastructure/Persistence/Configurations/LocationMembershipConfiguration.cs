using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class LocationMembershipConfiguration : IEntityTypeConfiguration<LocationMembership>
{
    public void Configure(EntityTypeBuilder<LocationMembership> builder)
    {
        builder.ToTable("location_memberships");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => new { x.LocationId, x.Status });

        // FR-03: employee can have at most one Active membership at a time
        builder.HasIndex(x => x.EmployeeId)
            .HasFilter("\"Status\" = 1")
            .IsUnique()
            .HasDatabaseName("IX_location_memberships_employee_active_unique");

        // FR-07: prevent duplicate Pending/Active request for same employee+location
        builder.HasIndex(x => new { x.EmployeeId, x.LocationId })
            .HasFilter("\"Status\" IN (0, 1)")
            .IsUnique()
            .HasDatabaseName("IX_location_memberships_employee_location_pending_active_unique");

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
