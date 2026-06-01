using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class OrgJoinRequestConfiguration : IEntityTypeConfiguration<OrgJoinRequest>
{
    public void Configure(EntityTypeBuilder<OrgJoinRequest> builder)
    {
        builder.ToTable("org_join_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RejectNote).HasMaxLength(500);

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(OrgJoinRequestStatus.Pending)}'");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
