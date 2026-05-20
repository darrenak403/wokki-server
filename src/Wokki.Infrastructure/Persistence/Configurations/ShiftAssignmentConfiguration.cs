using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("shift_assignments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ScheduleId, x.ShiftDefinitionId, x.EmployeeId, x.Date }).IsUnique();

        builder.HasOne<Schedule>()
            .WithMany()
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShiftDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ShiftDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
