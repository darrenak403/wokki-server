using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.ToTable("overtime_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.Status);

        // Prevent duplicate active OT for same shift+employee (DB-level enforcement)
        // The composite (ShiftAssignmentId, EmployeeId) index below also covers single-column
        // lookups on ShiftAssignmentId via its leftmost-prefix, so no standalone index is needed.
        builder.HasIndex(x => new { x.ShiftAssignmentId, x.EmployeeId })
            .HasFilter("\"Status\" IN (0, 1)")
            .IsUnique()
            .HasDatabaseName("IX_overtime_requests_active_unique");

        builder.HasOne(x => x.ShiftAssignment)
            .WithMany()
            .HasForeignKey(x => x.ShiftAssignmentId)
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
