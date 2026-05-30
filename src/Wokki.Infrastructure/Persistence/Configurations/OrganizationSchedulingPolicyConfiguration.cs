using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;
using Wokki.Infrastructure.Persistence.Configurations;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class OrganizationSchedulingPolicyConfiguration : IEntityTypeConfiguration<OrganizationSchedulingPolicy>
{
    public void Configure(EntityTypeBuilder<OrganizationSchedulingPolicy> builder)
    {
        builder.ToTable("organization_scheduling_policies");
        builder.HasKey(x => x.OrganizationId);
        builder.Property(x => x.RulesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SchemaVersion).HasMaxLength(64).IsRequired();

        builder.HasOne<Organization>()
            .WithOne()
            .HasForeignKey<OrganizationSchedulingPolicy>(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
