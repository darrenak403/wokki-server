using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class OrganizationSubscriptionLedgerEntryConfiguration
    : IEntityTypeConfiguration<OrganizationSubscriptionLedgerEntry>
{
    public void Configure(EntityTypeBuilder<OrganizationSubscriptionLedgerEntry> builder)
    {
        builder.ToTable("organization_subscription_ledger_entries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(60).IsRequired();
        builder.Property(x => x.PreviousStatus).HasMaxLength(40).IsRequired();
        builder.Property(x => x.NewStatus).HasMaxLength(40).IsRequired();
        builder.Property(x => x.PreviousExpiresAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.NewExpiresAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ChangedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.BeforeJson).HasColumnType("jsonb");
        builder.Property(x => x.AfterJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.OrganizationId, x.ChangedAt }).IsDescending(false, true);
        builder.HasIndex(x => x.ChangedByUserId);
        builder.HasIndex(x => x.Action);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
