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

        builder.HasOne<ShiftAssignment>()
            .WithMany()
            .HasForeignKey(x => x.RequesterAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ShiftAssignment>()
            .WithMany()
            .HasForeignKey(x => x.TargetAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
