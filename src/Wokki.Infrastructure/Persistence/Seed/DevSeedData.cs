using Wokki.Domain.Constants;

namespace Wokki.Infrastructure.Persistence.Seed;

/// <summary>
/// Complete dev seed manifest (stable IDs + tabular rows).
/// Password for every account: <see cref="DevPassword"/>.
/// </summary>
public static class DevSeedData
{
    public const string DevPassword = "12345@Abc";
    public const string TimeZoneId = "Asia/Ho_Chi_Minh";

    // ── Stable IDs (smoke tests, FE docs) ─────────────────────────────────────

    public static readonly Guid LocationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid DepartmentBarId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid DepartmentBrewId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static readonly Guid UserAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid UserManagerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid UserDemoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UserBarista1Id = Guid.Parse("77777771-7777-7777-7777-777777777771");
    public static readonly Guid UserBarista2Id = Guid.Parse("77777772-7777-7777-7777-777777777772");
    public static readonly Guid UserBarista3Id = Guid.Parse("77777773-7777-7777-7777-777777777773");
    public static readonly Guid UserBarista4Id = Guid.Parse("77777774-7777-7777-7777-777777777774");
    public static readonly Guid UserBarista5Id = Guid.Parse("77777775-7777-7777-7777-777777777775");

    public static readonly Guid EmployeeAdminId = Guid.Parse("55555551-5555-5555-5555-555555555551");
    public static readonly Guid EmployeeManagerId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid EmployeeDemoId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid EmployeeBarista1Id = Guid.Parse("88888881-8888-8888-8888-888888888881");
    public static readonly Guid EmployeeBarista2Id = Guid.Parse("88888882-8888-8888-8888-888888888882");
    public static readonly Guid EmployeeBarista3Id = Guid.Parse("88888883-8888-8888-8888-888888888883");
    public static readonly Guid EmployeeBarista4Id = Guid.Parse("88888884-8888-8888-8888-888888888884");
    public static readonly Guid EmployeeBrewLeadId = Guid.Parse("88888886-8888-8888-8888-888888888886");

    public static readonly Guid ShiftMorningId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1");
    public static readonly Guid ShiftAfternoonId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2");
    public static readonly Guid ShiftClosingId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee3");

    public static readonly Guid ScheduleBarId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid PayPeriodId = Guid.Parse("12121212-1212-1212-1212-121212121212");

    public static readonly Guid AssignmentUserTodayId = Guid.Parse("f1111111-1111-1111-1111-111111111111");
    public static readonly Guid AssignmentManagerTodayId = Guid.Parse("f2222222-2222-2222-2222-222222222222");
    public static readonly Guid AssignmentOtSampleId = Guid.Parse("f3333333-3333-3333-3333-333333333333");

    public static readonly Guid SwapPendingId = Guid.Parse("99999991-9999-9999-9999-999999999991");
    public static readonly Guid ChannelGroupId = Guid.Parse("99999992-9999-9999-9999-999999999992");
    public static readonly Guid ChannelDirectId = Guid.Parse("99999993-9999-9999-9999-999999999993");

    public static readonly Guid OvertimePendingApprovalId = Guid.Parse("d1000001-0000-4000-8000-000000000001");
    public static readonly Guid LocationManagerId = Guid.Parse("e1000001-0000-4000-8000-000000000001");

    /// <summary>Transient holder for atomic assignment swaps (never scheduled).</summary>
    public static readonly Guid SwapHoldEmployeeId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    // ── users ───────────────────────────────────────────────────────────────────

    public static readonly UserRow[] Users =
    [
        new(UserAdminId, "admin@gmail.com", RoleConstants.Admin),
        new(UserManagerId, "manager@gmail.com", RoleConstants.Manager),
        new(UserDemoId, "user@gmail.com", RoleConstants.User),
        new(UserBarista1Id, "barista1@gmail.com", RoleConstants.User),
        new(UserBarista2Id, "barista2@gmail.com", RoleConstants.User),
        new(UserBarista3Id, "barista3@gmail.com", RoleConstants.User),
        new(UserBarista4Id, "barista4@gmail.com", RoleConstants.User),
        new(UserBarista5Id, "barista5@gmail.com", RoleConstants.User),
    ];

    // ── locations ───────────────────────────────────────────────────────────────

    public static readonly LocationRow[] Locations =
    [
        new(LocationId, "Wokki Coffê", "42 Nguyễn Huệ, Quận 1, TP.HCM", TimeZoneId),
    ];

    // ── departments ───────────────────────────────────────────────────────────

    public static readonly DepartmentRow[] Departments =
    [
        new(DepartmentBarId, LocationId, "Quầy bar"),
        new(DepartmentBrewId, LocationId, "Pha chế"),
    ];

    // ── employees ───────────────────────────────────────────────────────────────

    public static readonly EmployeeRow[] Employees =
    [
        new(EmployeeAdminId, UserAdminId, DepartmentBarId, "An", "Phạm", "0901000001", "Chủ quán", 0m),
        new(EmployeeManagerId, UserManagerId, DepartmentBarId, "Minh", "Trần", "0901000002", "Trưởng ca", 45_000m),
        new(EmployeeDemoId, UserDemoId, DepartmentBarId, "Lan", "Nguyễn", "0901000003", "Barista", 28_000m),
        new(EmployeeBarista1Id, UserBarista1Id, DepartmentBarId, "Hoa", "Lê", "0901000004", "Barista", 27_000m),
        new(EmployeeBarista2Id, UserBarista2Id, DepartmentBarId, "Khoa", "Phạm", "0901000005", "Thu ngân", 26_000m),
        new(EmployeeBarista3Id, UserBarista3Id, DepartmentBarId, "Vy", "Hoàng", "0901000006", "Barista", 27_500m),
        new(EmployeeBarista4Id, UserBarista4Id, DepartmentBarId, "An", "Đỗ", "0901000007", "Barista", 29_000m),
        new(EmployeeBrewLeadId, UserBarista5Id, DepartmentBrewId, "Bình", "Võ", "0901000008", "Trưởng pha chế", 32_000m),
    ];

    // ── location managers ───────────────────────────────────────────────────────

    public static readonly LocationManagerRow[] LocationManagers =
    [
        new(LocationManagerId, LocationId, UserManagerId, UserAdminId),
    ];

    // ── shift definitions ───────────────────────────────────────────────────────

    public static readonly ShiftDefinitionRow[] ShiftDefinitions =
    [
        new(ShiftMorningId, LocationId, DepartmentBarId, "Ca sáng", new(6, 0), new(14, 0), "#F59E0B"),
        new(ShiftAfternoonId, LocationId, DepartmentBarId, "Ca chiều", new(14, 0), new(22, 0), "#3B82F6"),
        new(ShiftClosingId, LocationId, DepartmentBarId, "Ca kín", new(22, 0), new(23, 0), "#6B7280"),
    ];

    // ── fixed shift assignments (today + OT sample) ─────────────────────────────

    public static readonly FixedAssignmentRow[] FixedAssignments =
    [
        new(AssignmentUserTodayId, EmployeeDemoId, ShiftMorningId, "Ca chính — demo user"),
        new(AssignmentManagerTodayId, EmployeeManagerId, ShiftAfternoonId, "Trưởng ca — demo manager"),
        new(AssignmentOtSampleId, EmployeeBarista1Id, ShiftMorningId, "Ca OT mẫu — đã kết thúc"),
    ];

    // ── swap requests ───────────────────────────────────────────────────────────

    public static readonly SwapRequestRow[] SwapRequests =
    [
        new(
            SwapPendingId,
            AssignmentUserTodayId,
            AssignmentManagerTodayId,
            EmployeeDemoId,
            EmployeeManagerId,
            "Đổi ca chiều tối nay được không?"),
    ];

    // ── chat ────────────────────────────────────────────────────────────────────

    public static readonly ChannelRow[] Channels =
    [
        new(ChannelGroupId, "Ca sáng — Wokki Coffê", ChannelTypeSeed.Group, UserManagerId),
        new(ChannelDirectId, null, ChannelTypeSeed.Direct, UserManagerId),
    ];

    public static readonly ChannelMemberRow[] ChannelMembers =
    [
        new(ChannelGroupId, EmployeeManagerId),
        new(ChannelGroupId, EmployeeDemoId),
        new(ChannelGroupId, EmployeeBarista1Id),
        new(ChannelGroupId, EmployeeBarista2Id),
        new(ChannelDirectId, EmployeeManagerId),
        new(ChannelDirectId, EmployeeDemoId),
    ];

    public static readonly MessageRow[] Messages =
    [
        new(Guid.Parse("c3000001-0000-4000-8000-000000000001"), ChannelGroupId, EmployeeManagerId, "Ca sáng mai 6h có mặt đủ nhé!", 0),
        new(Guid.Parse("c3000002-0000-4000-8000-000000000002"), ChannelGroupId, EmployeeDemoId, "Dạ em confirm ạ.", 1),
        new(Guid.Parse("c3000003-0000-4000-8000-000000000003"), ChannelDirectId, EmployeeManagerId, "Lan, nhớ kiểm tra máy pha sữa nhé.", 0),
    ];

    // ── bar staff rotation (week schedule, excludes today) ──────────────────────

    public static readonly Guid[] BarStaffRotation =
    [
        EmployeeDemoId,
        EmployeeBarista1Id,
        EmployeeBarista2Id,
        EmployeeBarista3Id,
        EmployeeBarista4Id,
    ];

    public static readonly Guid[] WeekShiftRotation = [ShiftMorningId, ShiftAfternoonId, ShiftClosingId];

    // ── row types ───────────────────────────────────────────────────────────────

    public sealed record UserRow(Guid Id, string Email, string Role);

    public sealed record LocationRow(Guid Id, string Name, string Address, string TimeZone);

    public sealed record DepartmentRow(Guid Id, Guid LocationId, string Name);

    public sealed record EmployeeRow(
        Guid Id,
        Guid UserId,
        Guid DepartmentId,
        string FirstName,
        string LastName,
        string Phone,
        string Position,
        decimal HourlyRate);

    public sealed record LocationManagerRow(Guid Id, Guid LocationId, Guid UserId, Guid AssignedById);

    public sealed record ShiftDefinitionRow(
        Guid Id,
        Guid LocationId,
        Guid DepartmentId,
        string Name,
        TimeOnly Start,
        TimeOnly End,
        string Color);

    public sealed record FixedAssignmentRow(
        Guid Id,
        Guid EmployeeId,
        Guid ShiftDefinitionId,
        string? Note);

    public sealed record SwapRequestRow(
        Guid Id,
        Guid RequesterAssignmentId,
        Guid TargetAssignmentId,
        Guid RequesterId,
        Guid TargetEmployeeId,
        string RequesterNote);

    public enum ChannelTypeSeed { Group, Direct }

    public sealed record ChannelRow(Guid Id, string? Name, ChannelTypeSeed Type, Guid CreatedByUserId);

    public sealed record ChannelMemberRow(Guid ChannelId, Guid EmployeeId);

    public sealed record MessageRow(Guid Id, Guid ChannelId, Guid SenderEmployeeId, string Body, int MinutesAfterBase);
}
