using System.Text.Json;
using Wokki.Application.Dtos.Location;
using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

public static class LocationSchedulingPolicyRules
{
    public const string SchemaVersion = "location-scheduling-policy.v2";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyList<LocationSchedulingRuleDto> DefaultRules =
    [
        Number("max_hours_per_day", "workHourLimits", "Nhân viên không làm quá số giờ này trong một ngày.", "Giờ tối đa / ngày", 8, true, 10),
        Number("max_hours_per_week", "workHourLimits", "Nhân viên không làm quá số giờ này trong một tuần.", "Giờ tối đa / tuần", 48, true, 20),
        Number("max_shifts_per_day", "workHourLimits", "Số ca tối đa một nhân viên được xếp trong một ngày.", "Ca tối đa / ngày", 1, true, 30),
        Number("max_shifts_per_week", "workHourLimits", "Số ca tối đa một nhân viên được xếp trong một tuần.", "Ca tối đa / tuần", 6, true, 40),
        Number("min_shifts_per_week", "workHourLimits", "Mục tiêu số ca tối thiểu cho mỗi nhân viên trong tuần.", "Ca tối thiểu / tuần", 0, false, 50),
        Bool("allow_overtime", "workHourLimits", "Chi nhánh có cho phép gợi ý lịch tạo overtime hay không.", "Cho phép tăng ca", false, false, 60),

        Number("min_rest_minutes_between_shifts", "restRules", "Khoảng nghỉ tối thiểu giữa hai ca của cùng nhân viên.", "Phút nghỉ giữa ca", 720, true, 110),
        Number("weekly_rest_days_required", "restRules", "Số ngày nghỉ cần có trong một tuần.", "Ngày nghỉ / tuần", 1, false, 120),
        Number("max_consecutive_work_days", "restRules", "Số ngày làm liên tiếp tối đa trước khi cần nghỉ.", "Ngày làm liên tiếp tối đa", 6, false, 130),
        Number("break_required_after_minutes", "restRules", "Sau số phút làm việc này thì cần nghỉ giữa ca.", "Nghỉ sau số phút làm", 360, false, 140),
        Number("break_minutes", "restRules", "Thời lượng nghỉ giữa ca.", "Số phút nghỉ", 30, false, 150),

        Bool("require_full_coverage", "coverageRules", "Solver chỉ nên trả lịch đủ người theo capacity.", "Yêu cầu đủ người", true, false, 210),
        Bool("allow_understaffed_suggestions", "coverageRules", "Cho phép solver trả gợi ý thiếu người khi không đủ dữ liệu.", "Cho phép gợi ý thiếu người", false, false, 220),
        Bool("coverage_by_role_required", "coverageRules", "Coverage phải xét theo vai trò/position của ca.", "Yêu cầu đủ theo vai trò", true, false, 230),
        Number("default_min_staff_per_shift", "coverageRules", "Số người tối thiểu mặc định nếu ca không có rule riêng.", "Số người tối thiểu / ca", 1, false, 240),
        Number("default_max_staff_per_shift", "coverageRules", "Số người tối đa mặc định nếu cần giới hạn thêm.", "Số người tối đa / ca", null, false, 250),

        Bool("require_department_membership", "employeeEligibilityRules", "Nhân viên phải thuộc phòng ban đang xếp lịch.", "Bắt buộc thuộc phòng ban", true, true, 310),
        Bool("require_role_match", "employeeEligibilityRules", "Position nhân viên phải khớp RequiredRole của ca.", "Bắt buộc khớp vai trò", true, false, 320),
        Bool("require_active_employee", "employeeEligibilityRules", "Chỉ xếp nhân viên đang active.", "Chỉ nhân viên active", true, true, 330),
        Bool("allow_terminated_employees", "employeeEligibilityRules", "Có cho phép nhân viên đã nghỉ xuất hiện trong gợi ý hay không.", "Cho phép nhân viên đã nghỉ", false, false, 340),

        Bool("require_submitted_preferences", "preferenceRules", "Yêu cầu có đăng ký ca đã gửi trước khi chạy auto-scheduling.", "Bắt buộc có đăng ký ca", true, true, 410),
        Bool("unavailable_is_hard_block", "preferenceRules", "Nếu nhân viên chọn unavailable thì solver không được xếp ca đó.", "Unavailable là chặn cứng", true, true, 420),
        Number("preferred_weight", "preferenceRules", "Điểm cộng khi gợi ý khớp ca preferred.", "Điểm ca preferred", 30, false, 430),
        Number("available_weight", "preferenceRules", "Điểm cộng khi dùng ca available.", "Điểm ca available", 5, false, 440),
        Number("missing_preference_penalty", "preferenceRules", "Điểm phạt khi không có preference rõ ràng.", "Phạt khi thiếu preference", -3, false, 450),

        Bool("balance_shift_count", "fairnessRules", "Cân bằng số ca giữa nhân viên trong phòng ban.", "Cân bằng số ca", true, false, 510),
        Number("fairness_weight", "fairnessRules", "Trọng số ưu tiên công bằng.", "Trọng số công bằng", 10, false, 520),
        Bool("balance_weekend_shifts", "fairnessRules", "Cân bằng số ca cuối tuần.", "Cân bằng ca cuối tuần", false, false, 530),
        Bool("avoid_same_employee_always_same_shift", "fairnessRules", "Tránh để một nhân viên luôn lặp cùng loại ca.", "Tránh luôn cùng một ca", true, false, 540),

        Bool("avoid_overtime", "costRules", "Giảm ưu tiên các phương án tạo overtime.", "Tránh overtime", true, false, 610),
        Number("overtime_penalty_weight", "costRules", "Điểm phạt khi phương án tạo overtime.", "Phạt overtime", 50, false, 620),
        Bool("prefer_lower_cost_when_equal", "costRules", "Khi điểm bằng nhau, ưu tiên nhân viên/ca có chi phí thấp hơn.", "Ưu tiên chi phí thấp khi bằng điểm", false, false, 630),

        Bool("require_manager_review_before_apply", "publishRules", "Manager/Admin phải review trước khi apply gợi ý.", "Bắt buộc review trước apply", true, true, 710),
        Bool("auto_apply_suggestions", "publishRules", "Cho phép tự động apply gợi ý sau khi solver chạy.", "Tự apply gợi ý", false, true, 720),
        Bool("allow_partial_apply", "publishRules", "Cho phép manager chỉ apply một phần gợi ý.", "Cho phép apply một phần", true, false, 730)
    ];

    public static IReadOnlyList<LocationSchedulingRuleDto> GetDefaultRules() => DefaultRules;

    public static IReadOnlyList<LocationSchedulingRuleDto> GetEffectiveRules(LocationSchedulingPolicy policy)
    {
        var stored = Deserialize(policy.RulesJson);
        if (stored.Count == 0)
            return DefaultRules;

        var defaultsByKey = DefaultRules.ToDictionary(rule => rule.Key, StringComparer.OrdinalIgnoreCase);
        return stored
            .Select(rule => defaultsByKey.TryGetValue(rule.Key, out var definition)
                ? rule with
                {
                    Category = string.IsNullOrWhiteSpace(rule.Category) ? definition.Category : rule.Category,
                    Content = string.IsNullOrWhiteSpace(rule.Content) ? definition.Content : rule.Content,
                    InputLabel = string.IsNullOrWhiteSpace(rule.InputLabel) ? definition.InputLabel : rule.InputLabel,
                    ValueType = string.IsNullOrWhiteSpace(rule.ValueType) ? definition.ValueType : rule.ValueType,
                    IsDefault = true
                }
                : rule)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.InputLabel)
            .ToList();
    }

    public static string Serialize(IReadOnlyList<LocationSchedulingRuleDto> rules) =>
        JsonSerializer.Serialize(rules, JsonOptions);

    public static string SerializeUpsert(IReadOnlyList<LocationSchedulingRuleUpsertDto> rules)
    {
        var normalized = NormalizeUpsert(rules);
        return Serialize(normalized);
    }

    public static IReadOnlyList<LocationSchedulingRuleDto> NormalizeUpsert(
        IReadOnlyList<LocationSchedulingRuleUpsertDto> rules)
    {
        var defaultsByKey = DefaultRules.ToDictionary(rule => rule.Key, StringComparer.OrdinalIgnoreCase);
        return rules
            .Select((rule, index) =>
            {
                var key = string.IsNullOrWhiteSpace(rule.Key)
                    ? $"custom_{Guid.NewGuid():N}"
                    : NormalizeKey(rule.Key);
                defaultsByKey.TryGetValue(key, out var definition);
                var valueType = NormalizeValueType(rule.ValueType, definition?.ValueType);
                return new LocationSchedulingRuleDto(
                    key,
                    string.IsNullOrWhiteSpace(rule.Category) ? definition?.Category ?? "customRules" : rule.Category.Trim(),
                    string.IsNullOrWhiteSpace(rule.Content) ? definition?.Content ?? rule.InputLabel.Trim() : rule.Content.Trim(),
                    string.IsNullOrWhiteSpace(rule.InputLabel) ? definition?.InputLabel ?? rule.Content.Trim() : rule.InputLabel.Trim(),
                    valueType,
                    NormalizeValue(rule.Value, valueType),
                    rule.Enabled,
                    definition is not null,
                    definition?.IsRequired ?? rule.IsRequired,
                    rule.SortOrder == 0 ? (definition?.SortOrder ?? 10_000 + index) : rule.SortOrder);
            })
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.InputLabel)
            .ToList();
    }

    public static bool TryValidate(IReadOnlyList<LocationSchedulingRuleUpsertDto> rules)
    {
        if (rules.Count > 100)
            return false;

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in NormalizeUpsert(rules))
        {
            if (!keys.Add(rule.Key)
                || string.IsNullOrWhiteSpace(rule.Content)
                || string.IsNullOrWhiteSpace(rule.InputLabel)
                || !IsSupportedValueType(rule.ValueType)
                || !ValueMatchesType(rule.Value, rule.ValueType))
                return false;
        }

        return true;
    }

    public static int GetInt(LocationSchedulingPolicy? policy, string key, int fallback) =>
        policy is null ? fallback : TryGetRule(policy, key, out var rule) ? ReadInt(rule.Value, fallback) : fallback;

    public static bool GetBool(LocationSchedulingPolicy? policy, string key, bool fallback) =>
        policy is null ? fallback : TryGetRule(policy, key, out var rule) ? ReadBool(rule.Value, fallback) : fallback;

    private static bool TryGetRule(LocationSchedulingPolicy policy, string key, out LocationSchedulingRuleDto rule)
    {
        rule = GetEffectiveRules(policy)
            .FirstOrDefault(item => item.Enabled && item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))!;
        return rule is not null;
    }

    private static IReadOnlyList<LocationSchedulingRuleDto> Deserialize(string rulesJson)
    {
        if (string.IsNullOrWhiteSpace(rulesJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<LocationSchedulingRuleDto>>(rulesJson, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static LocationSchedulingRuleDto Number(
        string key,
        string category,
        string content,
        string inputLabel,
        int? defaultValue,
        bool required,
        int sortOrder) =>
        new(key, category, content, inputLabel, "number", ToJsonElement(defaultValue), true, true, required, sortOrder);

    private static LocationSchedulingRuleDto Bool(
        string key,
        string category,
        string content,
        string inputLabel,
        bool defaultValue,
        bool required,
        int sortOrder) =>
        new(key, category, content, inputLabel, "boolean", ToJsonElement(defaultValue), true, true, required, sortOrder);

    private static JsonElement? ToJsonElement<T>(T value) =>
        JsonSerializer.SerializeToElement(value, JsonOptions);

    private static string NormalizeKey(string key) =>
        key.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');

    private static string NormalizeValueType(string valueType, string? fallback) =>
        IsSupportedValueType(valueType) ? valueType.Trim().ToLowerInvariant() : fallback ?? "text";

    private static bool IsSupportedValueType(string valueType) =>
        valueType.Trim().Equals("number", StringComparison.OrdinalIgnoreCase)
        || valueType.Trim().Equals("boolean", StringComparison.OrdinalIgnoreCase)
        || valueType.Trim().Equals("text", StringComparison.OrdinalIgnoreCase);

    private static JsonElement? NormalizeValue(JsonElement? value, string valueType)
    {
        if (value is null)
            return null;

        return valueType switch
        {
            "number" => ToJsonElement(ReadInt(value, 0)),
            "boolean" => ToJsonElement(ReadBool(value, false)),
            _ => ToJsonElement(value.Value.ValueKind == JsonValueKind.String
                ? value.Value.GetString() ?? string.Empty
                : value.Value.ToString())
        };
    }

    private static bool ValueMatchesType(JsonElement? value, string valueType) =>
        value is null
        || valueType switch
        {
            "number" => value.Value.ValueKind is JsonValueKind.Number or JsonValueKind.Null,
            "boolean" => value.Value.ValueKind is JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null,
            "text" => value.Value.ValueKind is JsonValueKind.String or JsonValueKind.Null,
            _ => false
        };

    private static int ReadInt(JsonElement? value, int fallback)
    {
        if (value is null)
            return fallback;
        if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetInt32(out var result))
            return result;
        if (value.Value.ValueKind == JsonValueKind.String && int.TryParse(value.Value.GetString(), out result))
            return result;
        return fallback;
    }

    private static bool ReadBool(JsonElement? value, bool fallback)
    {
        if (value is null)
            return fallback;
        if (value.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return value.Value.GetBoolean();
        if (value.Value.ValueKind == JsonValueKind.String && bool.TryParse(value.Value.GetString(), out var result))
            return result;
        return fallback;
    }
}
