using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class SchedulePreferenceLineConfiguration : IEntityTypeConfiguration<SchedulePreferenceLine>
{
    public void Configure(EntityTypeBuilder<SchedulePreferenceLine> builder)
    {
        builder.ToTable("schedule_preference_lines");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.SubmissionId, x.ShiftDefinitionId, x.Date }).IsUnique();

        builder.HasOne<ShiftDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ShiftDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasRequiredOrganization(x => x.OrganizationId);
    }
}
