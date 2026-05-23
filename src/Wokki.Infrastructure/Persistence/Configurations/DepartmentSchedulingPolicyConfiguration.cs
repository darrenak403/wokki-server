using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class DepartmentSchedulingPolicyConfiguration : IEntityTypeConfiguration<DepartmentSchedulingPolicy>
{
    public void Configure(EntityTypeBuilder<DepartmentSchedulingPolicy> builder)
    {
        builder.ToTable("department_scheduling_policies");
        builder.HasKey(x => x.DepartmentId);

        builder.HasOne<Department>()
            .WithOne()
            .HasForeignKey<DepartmentSchedulingPolicy>(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
