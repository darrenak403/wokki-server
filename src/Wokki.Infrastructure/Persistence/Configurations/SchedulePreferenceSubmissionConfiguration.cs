using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class SchedulePreferenceSubmissionConfiguration : IEntityTypeConfiguration<SchedulePreferenceSubmission>
{
    public void Configure(EntityTypeBuilder<SchedulePreferenceSubmission> builder)
    {
        builder.ToTable("schedule_preference_submissions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ScheduleId, x.EmployeeId }).IsUnique();

        builder.HasOne<Schedule>()
            .WithMany()
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
