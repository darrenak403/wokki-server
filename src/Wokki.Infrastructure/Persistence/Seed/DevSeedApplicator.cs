using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Infrastructure.Persistence.Seed;

public static class DevSeedApplicator
{
    public static async Task ApplyAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var nowUtc = DateTime.UtcNow;
            var today = TodayInVietnam();
            var weekStart = GetWeekStartMonday(today);
            var weekEnd = weekStart.AddDays(6);
            var passwordHash = passwordHasher.HashPassword(DevSeedData.DevPassword);
            var tz = ResolveTimeZone(DevSeedData.TimeZoneId);

            ApplyUsers(context, passwordHash, nowUtc);
            ApplyLocations(context, nowUtc);
            ApplyDepartments(context, nowUtc);
            ApplyEmployees(context, nowUtc);
            ApplyLocationManagers(context, nowUtc);
            ApplyLocationMemberships(context, nowUtc);
            ApplyDepartmentMemberships(context, nowUtc);
            ApplySchedulingPolicy(context, nowUtc);
            ApplyShiftDefinitions(context, nowUtc);

            var weekAssignments = ApplyWeekSchedule(context, weekStart, today, nowUtc);
            var fixedAssignments = ApplyFixedAssignments(context, today, nowUtc);
            var allAssignments = weekAssignments.Concat(fixedAssignments).ToList();

            ApplyPayPeriod(context, weekStart, weekEnd, nowUtc);
            ApplyAttendance(context, allAssignments, today, tz, nowUtc);
            ApplyOvertimeSamples(context, today, tz, nowUtc);
            ApplySwaps(context, nowUtc);
            ApplyChat(context, nowUtc);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Dev seed applied (week {WeekStart}–{WeekEnd}, today {Today}, tz {TimeZone}).",
                weekStart,
                weekEnd,
                today,
                DevSeedData.TimeZoneId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static DateOnly TodayInVietnam()
    {
        var tz = ResolveTimeZone(DevSeedData.TimeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return DateOnly.FromDateTime(local);
    }

    public static DateOnly GetWeekStartMonday(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-offset);
    }

    private static void ApplyUsers(AppDbContext context, string passwordHash, DateTime nowUtc)
    {
        foreach (var row in DevSeedData.Users)
        {
            context.Users.Add(new User
            {
                Id = row.Id,
                Email = row.Email,
                PasswordHash = passwordHash,
                Role = row.Role,
                CreatedAt = nowUtc
            });
        }
    }

    private static void ApplyLocations(AppDbContext context, DateTime nowUtc)
    {
        foreach (var row in DevSeedData.Locations)
        {
            context.Locations.Add(new Location
            {
                Id = row.Id,
                Name = row.Name,
                Address = row.Address,
                TimeZone = row.TimeZone,
                IsActive = true,
                CreatedAt = nowUtc
            });
        }
    }

    private static void ApplyDepartments(AppDbContext context, DateTime nowUtc)
    {
        foreach (var row in DevSeedData.Departments)
        {
            context.Departments.Add(new Department
            {
                Id = row.Id,
                LocationId = row.LocationId,
                Name = row.Name,
                IsActive = true,
                CreatedAt = nowUtc
            });
        }
    }

    private static void ApplyEmployees(AppDbContext context, DateTime nowUtc)
    {
        foreach (var row in DevSeedData.Employees)
        {
            context.Employees.Add(new Employee
            {
                Id = row.Id,
                UserId = row.UserId,
                FirstName = row.FirstName,
                LastName = row.LastName,
                Phone = row.Phone,
                Position = row.Position,
                HourlyRate = row.HourlyRate,
                DepartmentId = row.DepartmentId,
                EmployedAt = nowUtc,
                CreatedAt = nowUtc
            });
        }
    }

    private static void ApplyLocationManagers(AppDbContext context, DateTime nowUtc)
    {
        foreach (var row in DevSeedData.LocationManagers)
        {
            context.LocationManagers.Add(new LocationManager
            {
                Id = row.Id,
                LocationId = row.LocationId,
                UserId = row.UserId,
                AssignedById = row.AssignedById,
                AssignedAt = nowUtc
            });
        }
    }

    private static void ApplyLocationMemberships(AppDbContext context, DateTime nowUtc)
    {
        foreach (var employee in DevSeedData.Employees)
        {
            var locationId = DevSeedData.Departments
                .First(d => d.Id == employee.DepartmentId)
                .LocationId;

            context.LocationMemberships.Add(new LocationMembership
            {
                Id = Guid.NewGuid(),
                LocationId = locationId,
                EmployeeId = employee.Id,
                Status = LocationMembershipStatus.Active,
                RequestedAt = nowUtc,
                ReviewedAt = nowUtc
            });
        }
    }

    private static void ApplyDepartmentMemberships(AppDbContext context, DateTime nowUtc)
    {
        foreach (var employee in DevSeedData.Employees)
        {
            context.EmployeeDepartmentMemberships.Add(new EmployeeDepartmentMembership
            {
                EmployeeId = employee.Id,
                DepartmentId = employee.DepartmentId,
                IsPrimary = true,
                Status = DepartmentMembershipStatus.Active,
                JoinedAt = nowUtc,
                CreatedAt = nowUtc
            });
        }
    }

    private static void ApplySchedulingPolicy(AppDbContext context, DateTime nowUtc)
    {
        context.LocationSchedulingPolicies.Add(new LocationSchedulingPolicy
        {
            LocationId = DevSeedData.LocationId,
            RulesJson = "[]",
            UpdatedAt = nowUtc
        });
    }

    private static void ApplyShiftDefinitions(AppDbContext context, DateTime nowUtc)
    {
        foreach (var row in DevSeedData.ShiftDefinitions)
        {
            context.ShiftDefinitions.Add(new ShiftDefinition
            {
                Id = row.Id,
                LocationId = row.LocationId,
                DepartmentId = row.DepartmentId,
                Name = row.Name,
                StartTime = row.Start,
                EndTime = row.End,
                RequiredRole = RoleConstants.User,
                Color = row.Color,
                IsActive = true,
                CreatedAt = nowUtc
            });
        }
    }

    private static List<AssignmentSeed> ApplyWeekSchedule(
        AppDbContext context,
        DateOnly weekStart,
        DateOnly today,
        DateTime nowUtc)
    {
        context.Schedules.Add(new Schedule
        {
            Id = DevSeedData.ScheduleBarId,
            DepartmentId = DevSeedData.DepartmentBarId,
            WeekStartDate = weekStart,
            Status = ScheduleStatus.Published,
            CreatedBy = DevSeedData.UserManagerId,
            PublishedAt = nowUtc,
            CreatedAt = nowUtc
        });

        var results = new List<AssignmentSeed>();
        var counter = 1;

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = weekStart.AddDays(dayOffset);
            if (date == today)
                continue;

            var employeeId = DevSeedData.BarStaffRotation[dayOffset % DevSeedData.BarStaffRotation.Length];
            var shiftId = DevSeedData.WeekShiftRotation[dayOffset % DevSeedData.WeekShiftRotation.Length];
            var assignmentId = Guid.Parse($"a1000000-0000-4000-8000-{counter:D12}");
            counter++;

            context.ShiftAssignments.Add(new ShiftAssignment
            {
                Id = assignmentId,
                ScheduleId = DevSeedData.ScheduleBarId,
                ShiftDefinitionId = shiftId,
                EmployeeId = employeeId,
                Date = date,
                CreatedAt = nowUtc
            });

            results.Add(new AssignmentSeed(assignmentId, employeeId, date, shiftId));
        }

        return results;
    }

    private static List<AssignmentSeed> ApplyFixedAssignments(
        AppDbContext context,
        DateOnly today,
        DateTime nowUtc)
    {
        var results = new List<AssignmentSeed>();
        var otSampleDate = today.AddDays(-1);

        foreach (var row in DevSeedData.FixedAssignments)
        {
            var date = row.Id == DevSeedData.AssignmentOtSampleId ? otSampleDate : today;

            context.ShiftAssignments.Add(new ShiftAssignment
            {
                Id = row.Id,
                ScheduleId = DevSeedData.ScheduleBarId,
                ShiftDefinitionId = row.ShiftDefinitionId,
                EmployeeId = row.EmployeeId,
                Date = date,
                Note = row.Note,
                CreatedAt = nowUtc
            });

            results.Add(new AssignmentSeed(row.Id, row.EmployeeId, date, row.ShiftDefinitionId));
        }

        return results;
    }

    private static void ApplyPayPeriod(
        AppDbContext context,
        DateOnly weekStart,
        DateOnly weekEnd,
        DateTime nowUtc)
    {
        context.PayPeriods.Add(new PayPeriod
        {
            Id = DevSeedData.PayPeriodId,
            DepartmentId = DevSeedData.DepartmentBarId,
            StartDate = weekStart,
            EndDate = weekEnd,
            Status = PayPeriodStatus.Open,
            CreatedAt = nowUtc
        });
    }

    private static void ApplyAttendance(
        AppDbContext context,
        List<AssignmentSeed> assignments,
        DateOnly today,
        TimeZoneInfo tz,
        DateTime nowUtc)
    {
        var shiftTimes = DevSeedData.ShiftDefinitions.ToDictionary(
            s => s.Id,
            s => (s.Start, s.End));

        var closedCandidates = assignments
            .Where(a => a.Date < today && a.EmployeeId != DevSeedData.EmployeeManagerId)
            .Take(5)
            .ToList();

        if (closedCandidates.Count < 3)
        {
            closedCandidates = assignments
                .Where(a => a.Date <= today && a.EmployeeId != DevSeedData.EmployeeManagerId)
                .Take(3)
                .ToList();
        }

        var index = 1;
        foreach (var assignment in closedCandidates)
        {
            var (startTime, endTime) = shiftTimes[assignment.ShiftDefinitionId];
            var clockIn = ShiftInstantUtc(assignment.Date, startTime, tz);
            var clockOut = ShiftInstantUtc(assignment.Date, endTime, tz);

            context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = Guid.Parse($"b2000000-0000-4000-8000-{index:D12}"),
                EmployeeId = assignment.EmployeeId,
                AssignmentId = assignment.AssignmentId,
                ClockIn = clockIn,
                ClockOut = clockOut,
                WorkedMinutes = (int)Math.Max(0, (clockOut - clockIn).TotalMinutes),
                CreatedAt = nowUtc
            });
            index++;
        }
    }

    private static void ApplyOvertimeSamples(
        AppDbContext context,
        DateOnly today,
        TimeZoneInfo tz,
        DateTime nowUtc)
    {
        var shift = DevSeedData.ShiftDefinitions.First(s => s.Id == DevSeedData.ShiftMorningId);
        var otDate = today.AddDays(-1);
        var shiftEndUtc = ShiftInstantUtc(otDate, shift.End, tz);
        var startedAt = shiftEndUtc.AddMinutes(15);
        var endedAt = startedAt.AddHours(1);

        context.OvertimeRequests.Add(new OvertimeRequest
        {
            Id = DevSeedData.OvertimePendingApprovalId,
            ShiftAssignmentId = DevSeedData.AssignmentOtSampleId,
            EmployeeId = DevSeedData.EmployeeBarista1Id,
            Reason = "Dọn kho và chốt ca — seed demo OT chờ duyệt.",
            StartedAt = startedAt,
            EndedAt = endedAt,
            OvertimeMinutes = (int)(endedAt - startedAt).TotalMinutes,
            Status = OvertimeStatus.PendingApproval,
            CreatedAt = nowUtc
        });
    }

    private static void ApplySwaps(AppDbContext context, DateTime nowUtc)
    {
        foreach (var row in DevSeedData.SwapRequests)
        {
            context.SwapRequests.Add(new SwapRequest
            {
                Id = row.Id,
                RequesterAssignmentId = row.RequesterAssignmentId,
                TargetAssignmentId = row.TargetAssignmentId,
                RequesterId = row.RequesterId,
                TargetEmployeeId = row.TargetEmployeeId,
                Status = SwapStatus.Pending,
                RequesterNote = row.RequesterNote,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            });
        }
    }

    private static void ApplyChat(AppDbContext context, DateTime nowUtc)
    {
        foreach (var row in DevSeedData.Channels)
        {
            context.Channels.Add(new Channel
            {
                Id = row.Id,
                Name = row.Name,
                Type = row.Type == DevSeedData.ChannelTypeSeed.Group ? ChannelType.Group : ChannelType.Direct,
                CreatedBy = row.CreatedByUserId,
                CreatedAt = nowUtc
            });
        }

        foreach (var row in DevSeedData.ChannelMembers)
        {
            context.ChannelMembers.Add(new ChannelMember
            {
                Id = Guid.NewGuid(),
                ChannelId = row.ChannelId,
                EmployeeId = row.EmployeeId,
                JoinedAt = nowUtc
            });
        }

        foreach (var row in DevSeedData.Messages)
        {
            context.Messages.Add(new Message
            {
                Id = row.Id,
                ChannelId = row.ChannelId,
                SenderId = row.SenderEmployeeId,
                Body = row.Body,
                CreatedAt = nowUtc.AddMinutes(row.MinutesAfterBase)
            });
        }
    }

    private static DateTimeOffset ShiftInstantUtc(DateOnly date, TimeOnly time, TimeZoneInfo tz)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, tz), TimeSpan.Zero);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    private sealed record AssignmentSeed(Guid AssignmentId, Guid EmployeeId, DateOnly Date, Guid ShiftDefinitionId);
}
