using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class EmployeeDepartmentMembershipConfiguration : IEntityTypeConfiguration<EmployeeDepartmentMembership>
{
    public void Configure(EntityTypeBuilder<EmployeeDepartmentMembership> builder)
    {
        builder.ToTable("employee_department_memberships");
        builder.HasKey(x => new { x.EmployeeId, x.DepartmentId });
        builder.HasIndex(x => x.DepartmentId);

        builder.Property(x => x.Status)
            .HasColumnType("integer")
            .HasDefaultValue(DepartmentMembershipStatus.Active)
            .HasSentinel(DepartmentMembershipStatus.None)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.LeftAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
