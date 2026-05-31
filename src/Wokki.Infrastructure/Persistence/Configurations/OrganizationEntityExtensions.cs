using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

internal static class OrganizationEntityExtensions
{
    public static void HasRequiredOrganization<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, Guid>> organizationId)
        where TEntity : class
    {
        builder.Property(organizationId).IsRequired();
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey("OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);
    }

    public static void HasOptionalOrganization<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, Guid?>> organizationId)
        where TEntity : class
    {
        builder.Property(organizationId).IsRequired(false);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey("OrganizationId")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
