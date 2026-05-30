using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class ScheduleLeaveRequestConfiguration : IEntityTypeConfiguration<ScheduleLeaveRequest>
{
    public void Configure(EntityTypeBuilder<ScheduleLeaveRequest> builder)
    {
        builder.ToTable("schedule_leave_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.ScheduleId);
        builder.HasIndex(x => x.EmployeeId);
        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => new { x.ScheduleId, x.EmployeeId, x.ShiftDefinitionId, x.Date })
            .HasFilter("\"Status\" = 0")
            .IsUnique()
            .HasDatabaseName("IX_schedule_leave_requests_pending_slot_unique");

        builder.HasRequiredOrganization(x => x.OrganizationId);
    }
}
