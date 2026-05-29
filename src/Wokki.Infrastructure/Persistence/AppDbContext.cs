using Microsoft.EntityFrameworkCore;
using Wokki.Domain.Entities;

namespace Wokki.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ShiftDefinition> ShiftDefinitions => Set<ShiftDefinition>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<SwapRequest> SwapRequests => Set<SwapRequest>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<PayPeriod> PayPeriods => Set<PayPeriod>();
    public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<EmployeeAvailability> EmployeeAvailabilities => Set<EmployeeAvailability>();
    public DbSet<LocationSchedulingPolicy> LocationSchedulingPolicies => Set<LocationSchedulingPolicy>();
    public DbSet<EmployeeDepartmentMembership> EmployeeDepartmentMemberships => Set<EmployeeDepartmentMembership>();
    public DbSet<SchedulePreferenceSubmission> SchedulePreferenceSubmissions => Set<SchedulePreferenceSubmission>();
    public DbSet<SchedulePreferenceLine> SchedulePreferenceLines => Set<SchedulePreferenceLine>();
    public DbSet<ScheduleInsightContext> ScheduleInsightContexts => Set<ScheduleInsightContext>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<LocationManager> LocationManagers => Set<LocationManager>();
    public DbSet<LocationMembership> LocationMemberships => Set<LocationMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
