using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wokki.Application.Common.Interfaces;
using Wokki.Domain.Constants;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;

namespace Wokki.Infrastructure.Persistence;

public static class CoffeeShopSeedBuilder
{
    public static async Task SeedAsync(
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
            var password = passwordHasher.HashPassword(CoffeeShopSeedIds.DevPassword);

            SeedUsers(context, password, nowUtc);
            SeedLocationAndDepartments(context, nowUtc);
            SeedEmployees(context, nowUtc);
            SeedShiftDefinitions(context, nowUtc);
            var assignmentIds = SeedScheduleAndAssignments(context, weekStart, today, nowUtc);
            SeedPayPeriodAndAttendance(context, weekStart, weekEnd, today, nowUtc, assignmentIds);
            SeedSwap(context, nowUtc);
            SeedChat(context, nowUtc);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Wokki Coffê demo seed completed (week {WeekStart}, today {Today} {TimeZone}).",
                weekStart,
                today,
                CoffeeShopSeedIds.TimeZoneId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static DateOnly TodayInVietnam()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(CoffeeShopSeedIds.TimeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return DateOnly.FromDateTime(local);
    }

    public static DateOnly GetWeekStartMonday(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-offset);
    }

    private static void SeedUsers(AppDbContext context, string passwordHash, DateTime nowUtc)
    {
        var users = new (Guid Id, string Email, string Role)[]
        {
            (CoffeeShopSeedIds.UserAdminId, "admin@gmail.com", RoleConstants.Admin),
            (CoffeeShopSeedIds.UserManagerId, "manager@gmail.com", RoleConstants.Manager),
            (CoffeeShopSeedIds.UserDemoId, "user@gmail.com", RoleConstants.User),
            (CoffeeShopSeedIds.UserBarista1Id, "barista1@gmail.com", RoleConstants.User),
            (CoffeeShopSeedIds.UserBarista2Id, "barista2@gmail.com", RoleConstants.User),
            (CoffeeShopSeedIds.UserBarista3Id, "barista3@gmail.com", RoleConstants.User),
            (CoffeeShopSeedIds.UserBarista4Id, "barista4@gmail.com", RoleConstants.User),
            (CoffeeShopSeedIds.UserBarista5Id, "barista5@gmail.com", RoleConstants.User),
        };

        foreach (var (id, email, role) in users)
        {
            context.Users.Add(new User
            {
                Id = id,
                Email = email,
                PasswordHash = passwordHash,
                Role = role,
                CreatedAt = nowUtc
            });
        }
    }

    private static void SeedLocationAndDepartments(AppDbContext context, DateTime nowUtc)
    {
        context.Locations.Add(new Location
        {
            Id = CoffeeShopSeedIds.LocationId,
            Name = "Wokki Coffê",
            Address = "42 Nguyễn Huệ, Quận 1, TP.HCM",
            TimeZone = CoffeeShopSeedIds.TimeZoneId,
            IsActive = true,
            CreatedAt = nowUtc
        });

        context.Departments.AddRange(
            new Department
            {
                Id = CoffeeShopSeedIds.DepartmentBarId,
                LocationId = CoffeeShopSeedIds.LocationId,
                Name = "Quầy bar",
                IsActive = true,
                CreatedAt = nowUtc
            },
            new Department
            {
                Id = CoffeeShopSeedIds.DepartmentBrewId,
                LocationId = CoffeeShopSeedIds.LocationId,
                Name = "Pha chế",
                IsActive = true,
                CreatedAt = nowUtc
            });
    }

    private static void SeedEmployees(AppDbContext context, DateTime nowUtc)
    {
        context.Employees.AddRange(
            new Employee
            {
                Id = CoffeeShopSeedIds.EmployeeManagerId,
                UserId = CoffeeShopSeedIds.UserManagerId,
                FirstName = "Minh",
                LastName = "Trần",
                Phone = "0901000002",
                Position = "Trưởng ca",
                HourlyRate = 45_000m,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                EmployedAt = nowUtc,
                CreatedAt = nowUtc
            },
            new Employee
            {
                Id = CoffeeShopSeedIds.EmployeeDemoId,
                UserId = CoffeeShopSeedIds.UserDemoId,
                FirstName = "Lan",
                LastName = "Nguyễn",
                Phone = "0901000003",
                Position = "Barista",
                HourlyRate = 28_000m,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                EmployedAt = nowUtc,
                CreatedAt = nowUtc
            },
            new Employee
            {
                Id = CoffeeShopSeedIds.EmployeeBarista1Id,
                UserId = CoffeeShopSeedIds.UserBarista1Id,
                FirstName = "Hoa",
                LastName = "Lê",
                Phone = "0901000004",
                Position = "Barista",
                HourlyRate = 27_000m,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                EmployedAt = nowUtc,
                CreatedAt = nowUtc
            },
            new Employee
            {
                Id = CoffeeShopSeedIds.EmployeeBarista2Id,
                UserId = CoffeeShopSeedIds.UserBarista2Id,
                FirstName = "Khoa",
                LastName = "Phạm",
                Phone = "0901000005",
                Position = "Thu ngân",
                HourlyRate = 26_000m,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                EmployedAt = nowUtc,
                CreatedAt = nowUtc
            },
            new Employee
            {
                Id = CoffeeShopSeedIds.EmployeeBarista3Id,
                UserId = CoffeeShopSeedIds.UserBarista3Id,
                FirstName = "Vy",
                LastName = "Hoàng",
                Phone = "0901000006",
                Position = "Barista",
                HourlyRate = 27_500m,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                EmployedAt = nowUtc,
                CreatedAt = nowUtc
            },
            new Employee
            {
                Id = CoffeeShopSeedIds.EmployeeBarista4Id,
                UserId = CoffeeShopSeedIds.UserBarista4Id,
                FirstName = "An",
                LastName = "Đỗ",
                Phone = "0901000007",
                Position = "Pha chế",
                HourlyRate = 29_000m,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                EmployedAt = nowUtc,
                CreatedAt = nowUtc
            },
            new Employee
            {
                Id = CoffeeShopSeedIds.EmployeeBrewLeadId,
                UserId = CoffeeShopSeedIds.UserBarista5Id,
                FirstName = "Bình",
                LastName = "Võ",
                Phone = "0901000008",
                Position = "Trưởng pha chế",
                HourlyRate = 32_000m,
                DepartmentId = CoffeeShopSeedIds.DepartmentBrewId,
                EmployedAt = nowUtc,
                CreatedAt = nowUtc
            });
    }

    private static void SeedShiftDefinitions(AppDbContext context, DateTime nowUtc)
    {
        context.ShiftDefinitions.AddRange(
            new ShiftDefinition
            {
                Id = CoffeeShopSeedIds.ShiftMorningId,
                LocationId = CoffeeShopSeedIds.LocationId,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                Name = "Ca sáng",
                StartTime = new TimeOnly(6, 0),
                EndTime = new TimeOnly(14, 0),
                RequiredRole = RoleConstants.User,
                Color = "#F59E0B",
                IsActive = true,
                CreatedAt = nowUtc
            },
            new ShiftDefinition
            {
                Id = CoffeeShopSeedIds.ShiftAfternoonId,
                LocationId = CoffeeShopSeedIds.LocationId,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                Name = "Ca chiều",
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(22, 0),
                RequiredRole = RoleConstants.User,
                Color = "#3B82F6",
                IsActive = true,
                CreatedAt = nowUtc
            },
            new ShiftDefinition
            {
                Id = CoffeeShopSeedIds.ShiftClosingId,
                LocationId = CoffeeShopSeedIds.LocationId,
                DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
                Name = "Ca kín",
                StartTime = new TimeOnly(22, 0),
                EndTime = new TimeOnly(23, 0),
                RequiredRole = RoleConstants.User,
                Color = "#6B7280",
                IsActive = true,
                CreatedAt = nowUtc
            });
    }

    private static List<(Guid AssignmentId, Guid EmployeeId, DateOnly Date, Guid ShiftDefinitionId)> SeedScheduleAndAssignments(
        AppDbContext context,
        DateOnly weekStart,
        DateOnly today,
        DateTime nowUtc)
    {
        context.Schedules.Add(new Schedule
        {
            Id = CoffeeShopSeedIds.ScheduleBarId,
            DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
            WeekStartDate = weekStart,
            Status = ScheduleStatus.Published,
            CreatedBy = CoffeeShopSeedIds.UserManagerId,
            PublishedAt = nowUtc,
            CreatedAt = nowUtc
        });

        var barStaff = new[]
        {
            CoffeeShopSeedIds.EmployeeDemoId,
            CoffeeShopSeedIds.EmployeeBarista1Id,
            CoffeeShopSeedIds.EmployeeBarista2Id,
            CoffeeShopSeedIds.EmployeeBarista3Id,
            CoffeeShopSeedIds.EmployeeBarista4Id,
        };

        var results = new List<(Guid AssignmentId, Guid EmployeeId, DateOnly Date, Guid ShiftDefinitionId)>();
        var assignmentCounter = 1;

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = weekStart.AddDays(dayOffset);
            if (date == today)
                continue;

            var employee = barStaff[dayOffset % barStaff.Length];
            var shiftId = (dayOffset % 3) switch
            {
                0 => CoffeeShopSeedIds.ShiftMorningId,
                1 => CoffeeShopSeedIds.ShiftAfternoonId,
                _ => CoffeeShopSeedIds.ShiftClosingId
            };

            var assignmentId = Guid.Parse($"a1000000-0000-4000-8000-{assignmentCounter:D12}");
            assignmentCounter++;

            context.ShiftAssignments.Add(new ShiftAssignment
            {
                Id = assignmentId,
                ScheduleId = CoffeeShopSeedIds.ScheduleBarId,
                ShiftDefinitionId = shiftId,
                EmployeeId = employee,
                Date = date,
                CreatedAt = nowUtc
            });

            results.Add((assignmentId, employee, date, shiftId));
        }

        context.ShiftAssignments.Add(new ShiftAssignment
        {
            Id = CoffeeShopSeedIds.AssignmentUserTodayId,
            ScheduleId = CoffeeShopSeedIds.ScheduleBarId,
            ShiftDefinitionId = CoffeeShopSeedIds.ShiftMorningId,
            EmployeeId = CoffeeShopSeedIds.EmployeeDemoId,
            Date = today,
            Note = "Ca chính — demo user",
            CreatedAt = nowUtc
        });
        results.Add((CoffeeShopSeedIds.AssignmentUserTodayId, CoffeeShopSeedIds.EmployeeDemoId, today, CoffeeShopSeedIds.ShiftMorningId));

        context.ShiftAssignments.Add(new ShiftAssignment
        {
            Id = CoffeeShopSeedIds.AssignmentManagerTodayId,
            ScheduleId = CoffeeShopSeedIds.ScheduleBarId,
            ShiftDefinitionId = CoffeeShopSeedIds.ShiftAfternoonId,
            EmployeeId = CoffeeShopSeedIds.EmployeeManagerId,
            Date = today,
            Note = "Trưởng ca — demo manager",
            CreatedAt = nowUtc
        });
        results.Add((CoffeeShopSeedIds.AssignmentManagerTodayId, CoffeeShopSeedIds.EmployeeManagerId, today, CoffeeShopSeedIds.ShiftAfternoonId));

        return results;
    }

    private static void SeedPayPeriodAndAttendance(
        AppDbContext context,
        DateOnly weekStart,
        DateOnly weekEnd,
        DateOnly today,
        DateTime nowUtc,
        List<(Guid AssignmentId, Guid EmployeeId, DateOnly Date, Guid ShiftDefinitionId)> assignments)
    {
        var shiftTimes = new Dictionary<Guid, (TimeOnly Start, TimeOnly End)>
        {
            [CoffeeShopSeedIds.ShiftMorningId] = (new TimeOnly(6, 0), new TimeOnly(14, 0)),
            [CoffeeShopSeedIds.ShiftAfternoonId] = (new TimeOnly(14, 0), new TimeOnly(22, 0)),
            [CoffeeShopSeedIds.ShiftClosingId] = (new TimeOnly(22, 0), new TimeOnly(23, 0)),
        };

        context.PayPeriods.Add(new PayPeriod
        {
            Id = CoffeeShopSeedIds.PayPeriodId,
            DepartmentId = CoffeeShopSeedIds.DepartmentBarId,
            StartDate = weekStart,
            EndDate = weekEnd,
            Status = PayPeriodStatus.Open,
            CreatedAt = nowUtc
        });

        var tz = TimeZoneInfo.FindSystemTimeZoneById(CoffeeShopSeedIds.TimeZoneId);
        var closedCandidates = assignments
            .Where(a => a.Date < today && a.EmployeeId != CoffeeShopSeedIds.EmployeeManagerId)
            .Take(5)
            .ToList();

        if (closedCandidates.Count < 3)
        {
            closedCandidates = assignments
                .Where(a => a.Date <= today && a.EmployeeId != CoffeeShopSeedIds.EmployeeManagerId)
                .Take(3)
                .ToList();
        }

        var attendanceIndex = 1;
        foreach (var (assignmentId, employeeId, date, shiftDefinitionId) in closedCandidates)
        {
            var (startTime, endTime) = shiftTimes[shiftDefinitionId];
            var localStart = date.ToDateTime(startTime);
            var localEnd = date.ToDateTime(endTime);
            // Npgsql timestamptz requires UTC offset 0 (same as AttendanceService.ClockInAsync).
            var clockIn = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, tz), TimeSpan.Zero);
            var clockOut = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, tz), TimeSpan.Zero);
            var workedMinutes = (int)(clockOut - clockIn).TotalMinutes;

            context.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = Guid.Parse($"b2000000-0000-4000-8000-{attendanceIndex:D12}"),
                EmployeeId = employeeId,
                AssignmentId = assignmentId,
                ClockIn = clockIn,
                ClockOut = clockOut,
                WorkedMinutes = workedMinutes,
                CreatedAt = nowUtc
            });
            attendanceIndex++;
        }
    }

    private static void SeedSwap(AppDbContext context, DateTime nowUtc)
    {
        context.SwapRequests.Add(new SwapRequest
        {
            Id = CoffeeShopSeedIds.SwapPendingId,
            RequesterAssignmentId = CoffeeShopSeedIds.AssignmentUserTodayId,
            TargetAssignmentId = CoffeeShopSeedIds.AssignmentManagerTodayId,
            RequesterId = CoffeeShopSeedIds.EmployeeDemoId,
            TargetEmployeeId = CoffeeShopSeedIds.EmployeeManagerId,
            Status = SwapStatus.Pending,
            RequesterNote = "Đổi ca chiều tối nay được không?",
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        });
    }

    private static void SeedChat(AppDbContext context, DateTime nowUtc)
    {
        context.Channels.Add(new Channel
        {
            Id = CoffeeShopSeedIds.ChannelGroupId,
            Name = "Ca sáng — Wokki Coffê",
            Type = ChannelType.Group,
            CreatedBy = CoffeeShopSeedIds.UserManagerId,
            CreatedAt = nowUtc
        });

        foreach (var employeeId in new[]
                 {
                     CoffeeShopSeedIds.EmployeeManagerId,
                     CoffeeShopSeedIds.EmployeeDemoId,
                     CoffeeShopSeedIds.EmployeeBarista1Id,
                     CoffeeShopSeedIds.EmployeeBarista2Id
                 })
        {
            context.ChannelMembers.Add(new ChannelMember
            {
                Id = Guid.NewGuid(),
                ChannelId = CoffeeShopSeedIds.ChannelGroupId,
                EmployeeId = employeeId,
                JoinedAt = nowUtc
            });
        }

        context.Channels.Add(new Channel
        {
            Id = CoffeeShopSeedIds.ChannelDirectId,
            Name = null,
            Type = ChannelType.Direct,
            CreatedBy = CoffeeShopSeedIds.UserManagerId,
            CreatedAt = nowUtc
        });

        context.ChannelMembers.AddRange(
            new ChannelMember
            {
                Id = Guid.NewGuid(),
                ChannelId = CoffeeShopSeedIds.ChannelDirectId,
                EmployeeId = CoffeeShopSeedIds.EmployeeManagerId,
                JoinedAt = nowUtc
            },
            new ChannelMember
            {
                Id = Guid.NewGuid(),
                ChannelId = CoffeeShopSeedIds.ChannelDirectId,
                EmployeeId = CoffeeShopSeedIds.EmployeeDemoId,
                JoinedAt = nowUtc
            });

        context.Messages.AddRange(
            new Message
            {
                Id = Guid.Parse("c3000001-0000-4000-8000-000000000001"),
                ChannelId = CoffeeShopSeedIds.ChannelGroupId,
                SenderId = CoffeeShopSeedIds.EmployeeManagerId,
                Body = "Ca sáng mai 6h có mặt đủ nhé!",
                CreatedAt = nowUtc
            },
            new Message
            {
                Id = Guid.Parse("c3000002-0000-4000-8000-000000000002"),
                ChannelId = CoffeeShopSeedIds.ChannelGroupId,
                SenderId = CoffeeShopSeedIds.EmployeeDemoId,
                Body = "Dạ em confirm ạ.",
                CreatedAt = nowUtc.AddMinutes(1)
            },
            new Message
            {
                Id = Guid.Parse("c3000003-0000-4000-8000-000000000003"),
                ChannelId = CoffeeShopSeedIds.ChannelDirectId,
                SenderId = CoffeeShopSeedIds.EmployeeManagerId,
                Body = "Lan, nhớ kiểm tra máy pha sữa nhé.",
                CreatedAt = nowUtc
            });
    }
}
