using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class EmployeeAvailabilityConfiguration : IEntityTypeConfiguration<EmployeeAvailability>
{
    public void Configure(EntityTypeBuilder<EmployeeAvailability> builder)
    {
        builder.ToTable("employee_availabilities");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EmployeeId, x.DayOfWeek, x.EffectiveFrom });

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
