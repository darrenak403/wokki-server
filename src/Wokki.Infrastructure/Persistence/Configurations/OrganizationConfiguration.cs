using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.SubscriptionEnabled).IsRequired();
        builder.Property(x => x.SubscriptionDurationDays).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.SubscriptionActivatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.SubscriptionExpiresAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.SubscriptionUpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => x.Name);
    }
}
