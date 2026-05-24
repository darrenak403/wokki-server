using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class EmployeeDepartmentMembershipConfiguration : IEntityTypeConfiguration<EmployeeDepartmentMembership>
{
    public void Configure(EntityTypeBuilder<EmployeeDepartmentMembership> builder)
    {
        builder.ToTable("employee_department_memberships");
        builder.HasKey(x => new { x.EmployeeId, x.DepartmentId });
        builder.HasIndex(x => x.DepartmentId);

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
