using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class SwapRequestConfiguration : IEntityTypeConfiguration<SwapRequest>
{
    public void Configure(EntityTypeBuilder<SwapRequest> builder)
    {
        builder.ToTable("swap_requests");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequesterAssignmentId);
        builder.HasIndex(x => x.TargetAssignmentId);

        builder.HasIndex(x => x.RequesterAssignmentId)
            .HasFilter("\"Status\" = 1")
            .IsUnique()
            .HasDatabaseName("IX_swap_requests_requester_assignment_peer_accepted");

        builder.HasIndex(x => x.TargetAssignmentId)
            .HasFilter("\"Status\" = 1")
            .IsUnique()
            .HasDatabaseName("IX_swap_requests_target_assignment_peer_accepted");

        builder.HasOne<ShiftAssignment>()
            .WithMany()
            .HasForeignKey(x => x.RequesterAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ShiftAssignment>()
            .WithMany()
            .HasForeignKey(x => x.TargetAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasRequiredOrganization(x => x.OrganizationId);
    }
}
