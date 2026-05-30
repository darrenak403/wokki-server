using System.Text.Json;
using Wokki.Application.Dtos.Scheduling;

namespace Wokki.Application.Scheduling;

public static class SchedulingRuleCatalog
{
    public const string SchemaVersion = "org-scheduling-policy.v1";
    public const int MaxAdvisoryRules = 20;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly IReadOnlyList<SchedulingRuleCatalogCategoryDto> Categories =
    [
        new("preferenceRules", "Đăng ký ca", "Chuẩn bị trước khi Manager chạy gợi ý lịch."),
        new("coverageRules", "Đủ người ca", "Số nhân viên tối thiểu mỗi ca khi ca không khai báo riêng."),
        new("employeeEligibilityRules", "Vai trò", "Xếp đúng vị trí công việc (Barista, Phục vụ…)."),
        new("customRules", "Luật ghi chú", "Ghi chú nội bộ; solver không đọc."),
    ];

    public static IReadOnlyList<SchedulingRuleCatalogEntryDto> EnforcedRules { get; } = BuildEnforcedRules();

    public static IReadOnlyDictionary<string, SchedulingRuleCatalogEntryDto> EnforcedByKey { get; } =
        EnforcedRules.ToDictionary(rule => rule.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<SchedulingRuleCatalogEntryDto> GetRules() => EnforcedRules;

    public static SchedulingRuleDto ToRuleDto(
        SchedulingRuleCatalogEntryDto entry,
        JsonElement? value = null,
        bool? enabled = null) =>
        new(
            entry.Key,
            entry.Category,
            entry.Content,
            entry.InputLabel,
            entry.ValueType,
            value ?? entry.DefaultValue,
            enabled ?? true,
            true,
            entry.IsRequired,
            entry.SortOrder,
            entry.Enforcement);

    public static IReadOnlyList<SchedulingRuleDto> GetDefaultRuleDtos() =>
        EnforcedRules.Select(entry => ToRuleDto(entry)).ToList();

    public static SchedulingRuleCatalogResponse ToResponse() =>
        new(SchemaVersion, Categories, EnforcedRules);

    private static IReadOnlyList<SchedulingRuleCatalogEntryDto> BuildEnforcedRules() =>
    [
        Bool("require_submitted_preferences", "preferenceRules",
            "Manager chỉ gợi ý lịch sau khi nhân viên đã gửi đăng ký ca trong tuần.",
            "Cần đăng ký ca trước", true, true, 10),
        Bool("unavailable_is_hard_block", "preferenceRules",
            "Không xếp ca nhân viên đã báo bận trên bảng đăng ký.",
            "Tôn trọng ca đã báo bận", true, true, 20),
        Number("default_min_staff_per_shift", "coverageRules",
            "Mặc định mỗi ca cần bao nhiêu người (nếu ca không khai báo riêng).",
            "Số người tối thiểu / ca", 1, false, 110),
        Bool("require_role_match", "employeeEligibilityRules",
            "Chỉ gợi ý nhân viên đúng vai trò ca (vd. Barista cho ca pha chế). Tắt khi cần xếp chéo vị trí.",
            "Xếp đúng vai trò ca", true, false, 210),
    ];

    private static SchedulingRuleCatalogEntryDto Bool(
        string key,
        string category,
        string content,
        string inputLabel,
        bool defaultValue,
        bool required,
        int sortOrder) =>
        new(key, category, content, inputLabel, "boolean", ToJsonElement(defaultValue), required, sortOrder, "enforced");

    private static SchedulingRuleCatalogEntryDto Number(
        string key,
        string category,
        string content,
        string inputLabel,
        int defaultValue,
        bool required,
        int sortOrder) =>
        new(key, category, content, inputLabel, "number", ToJsonElement(defaultValue), required, sortOrder, "enforced");

    private static JsonElement? ToJsonElement<T>(T value) =>
        JsonSerializer.SerializeToElement(value, JsonOptions);
}
