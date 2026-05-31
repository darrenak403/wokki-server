using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class ScheduleInsightContextConfiguration : IEntityTypeConfiguration<ScheduleInsightContext>
{
    public void Configure(EntityTypeBuilder<ScheduleInsightContext> builder)
    {
        builder.ToTable("schedule_insight_contexts");
        builder.HasKey(x => x.ScheduleId);
        builder.HasIndex(x => new { x.DepartmentId, x.WeekStartDate });
        builder.HasIndex(x => x.ExpiresAt);
        builder.Property(x => x.SchemaVersion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.JsonContent).HasColumnType("jsonb").IsRequired();

        builder.HasOne<Schedule>()
            .WithOne()
            .HasForeignKey<ScheduleInsightContext>(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasRequiredOrganization(x => x.OrganizationId);
    }
}
