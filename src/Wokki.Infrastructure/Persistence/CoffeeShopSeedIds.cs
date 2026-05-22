namespace Wokki.Infrastructure.Persistence;

/// <summary>Stable GUIDs for Wokki Coffê demo seed (smoke scripts, FE docs).</summary>
public static class CoffeeShopSeedIds
{
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

    public static readonly Guid EmployeeManagerId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid EmployeeDemoId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid EmployeeBarista1Id = Guid.Parse("88888881-8888-8888-8888-888888888881");
    public static readonly Guid EmployeeBarista2Id = Guid.Parse("88888882-8888-8888-8888-888888888882");
    public static readonly Guid EmployeeBarista3Id = Guid.Parse("88888883-8888-8888-8888-888888888883");
    public static readonly Guid EmployeeBarista4Id = Guid.Parse("88888884-8888-8888-8888-888888888884");
    /// <summary>barista5@gmail.com — Pha chế lead.</summary>
    public static readonly Guid EmployeeBrewLeadId = Guid.Parse("88888886-8888-8888-8888-888888888886");

    public static readonly Guid ShiftMorningId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1");
    public static readonly Guid ShiftAfternoonId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2");
    public static readonly Guid ShiftClosingId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee3");

    public static readonly Guid ScheduleBarId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid PayPeriodId = Guid.Parse("12121212-1212-1212-1212-121212121212");

    public static readonly Guid AssignmentUserTodayId = Guid.Parse("f1111111-1111-1111-1111-111111111111");
    public static readonly Guid AssignmentManagerTodayId = Guid.Parse("f2222222-2222-2222-2222-222222222222");

    public static readonly Guid SwapPendingId = Guid.Parse("99999991-9999-9999-9999-999999999991");
    public static readonly Guid ChannelGroupId = Guid.Parse("99999992-9999-9999-9999-999999999992");
    public static readonly Guid ChannelDirectId = Guid.Parse("99999993-9999-9999-9999-999999999993");

    public const string DevPassword = "12345@Abc";
    public const string TimeZoneId = "Asia/Ho_Chi_Minh";
}
