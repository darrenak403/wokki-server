using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence.Configurations;

public sealed class PayrollLineConfiguration : IEntityTypeConfiguration<PayrollLine>
{
    public void Configure(EntityTypeBuilder<PayrollLine> builder)
    {
        builder.ToTable("payroll_lines");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PayPeriodId, x.EmployeeId }).IsUnique();
        builder.Property(x => x.HourlyRate).HasPrecision(18, 2);
        builder.Property(x => x.GrossPay).HasPrecision(18, 2);
        builder.Property(x => x.OvertimePay).HasPrecision(18, 2);

        builder.HasOne<PayPeriod>()
            .WithMany()
            .HasForeignKey(x => x.PayPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
