using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class SwapPostConfiguration : IEntityTypeConfiguration<SwapPost>
{
    public void Configure(EntityTypeBuilder<SwapPost> builder)
    {
        builder.ToTable("swap_posts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasIndex(x => new { x.ScheduleId, x.DepartmentId, x.Status });
        builder.HasIndex(x => x.AuthorAssignmentId)
            .HasFilter("\"Status\" = 0")
            .IsUnique()
            .HasDatabaseName("IX_swap_posts_author_assignment_pending_unique");

        builder.HasOne<Schedule>()
            .WithMany()
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.AuthorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ShiftAssignment>()
            .WithMany()
            .HasForeignKey(x => x.AuthorAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.AcceptedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<ShiftAssignment>()
            .WithMany()
            .HasForeignKey(x => x.AcceptorAssignmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasRequiredOrganization(x => x.OrganizationId);
    }
}
