using System.Text.Json;
using Wokki.Application.Dtos.Location;
using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

public static class LocationSchedulingPolicyRules
{
    public const string SchemaVersion = "location-scheduling-policy.v3";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Core F&amp;B branch rules read by the heuristic scheduler. Custom branch rules may be appended.
    /// </summary>
    private static readonly IReadOnlyList<LocationSchedulingRuleDto> DefaultRules =
    [
        Number(
            "max_shifts_per_week",
            "employeeLimits",
            "Giới hạn số ca một nhân viên được xếp trong tuần lịch chính thức. Vượt mức này, hệ thống sẽ không gợi ý thêm ca cho người đó.",
            "Tối đa bao nhiêu ca / tuần?",
            6,
            true,
            10),
        Number(
            "max_shifts_per_day",
            "employeeLimits",
            "Một nhân viên không được xếp quá số ca này trong cùng một ngày — phù hợp quy định F&amp;B tránh xếp liên tục quá nhiều ca.",
            "Tối đa bao nhiêu ca / ngày?",
            1,
            true,
            20),
        Bool(
            "require_role_match",
            "employeeLimits",
            "Bật nếu ca thu ngân chỉ xếp nhân viên thu ngân, ca bếp chỉ xếp nhân viên bếp. Tắt nếu chi nhánh cho phép xếp chéo vai trò khi thiếu người.",
            "Chỉ xếp nhân viên đúng vai trò ca",
            true,
            true,
            30),

        Bool(
            "require_submitted_preferences",
            "preferenceRules",
            "Manager chỉ nên chạy gợi ý lịch sau khi nhân viên đã gửi bảng đăng ký ca (mong muốn / có thể làm). Tắt nếu muốn gợi ý ngay cả khi chưa có đăng ký.",
            "Bắt buộc có bảng đăng ký ca trước khi gợi ý",
            true,
            true,
            110),
        Bool(
            "unavailable_is_hard_block",
            "preferenceRules",
            "Nếu nhân viên đánh dấu không làm được ca đó trên bảng đăng ký, hệ thống sẽ không xếp vào lịch chính thức — kể cả khi ca còn thiếu người.",
            "Không xếp ca nhân viên đã báo bận",
            true,
            true,
            120)
    ];

    public static IReadOnlyList<LocationSchedulingRuleDto> GetDefaultRules() => DefaultRules;

    public static IReadOnlyList<LocationSchedulingRuleDto> GetEffectiveRules(LocationSchedulingPolicy policy)
    {
        var stored = Deserialize(policy.RulesJson);
        var storedByKey = stored.ToDictionary(rule => rule.Key, StringComparer.OrdinalIgnoreCase);
        var defaultKeys = DefaultRules.Select(rule => rule.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mergedDefaults = DefaultRules
            .Select(definition => storedByKey.TryGetValue(definition.Key, out var saved)
                ? definition with
                {
                    Value = saved.Value ?? definition.Value,
                    Enabled = saved.Enabled
                }
                : definition)
            .ToList();

        var customRules = stored
            .Where(rule => !defaultKeys.Contains(rule.Key))
            .Select(rule => rule with
            {
                Category = string.IsNullOrWhiteSpace(rule.Category) ? "customRules" : rule.Category,
                IsDefault = false
            })
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.InputLabel)
            .ToList();

        return mergedDefaults.Concat(customRules).ToList();
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
        var incomingByKey = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Key))
            .GroupBy(rule => NormalizeKey(rule.Key!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var mergedDefaults = DefaultRules
            .Select(definition =>
            {
                if (!incomingByKey.TryGetValue(definition.Key, out var incoming))
                    return definition;

                var valueType = NormalizeValueType(incoming.ValueType, definition.ValueType);
                return definition with
                {
                    Value = NormalizeValue(incoming.Value, valueType),
                    Enabled = incoming.Enabled
                };
            })
            .ToList();

        var customRules = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Key)
                && !defaultsByKey.ContainsKey(NormalizeKey(rule.Key!)))
            .Select((rule, index) => NormalizeCustomRule(rule, index, defaultsByKey))
            .Where(rule => rule is not null)
            .Cast<LocationSchedulingRuleDto>()
            .ToList();

        return mergedDefaults
            .Concat(customRules)
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

            if (rule.IsRequired && rule.ValueType == "number" && rule.Value is null)
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

    private static LocationSchedulingRuleDto? NormalizeCustomRule(
        LocationSchedulingRuleUpsertDto rule,
        int index,
        IReadOnlyDictionary<string, LocationSchedulingRuleDto> defaultsByKey)
    {
        if (string.IsNullOrWhiteSpace(rule.Key))
            return null;

        var key = NormalizeKey(rule.Key);
        if (defaultsByKey.ContainsKey(key))
            return null;

        var valueType = NormalizeValueType(rule.ValueType, "text");
        return new LocationSchedulingRuleDto(
            key.StartsWith("custom_", StringComparison.Ordinal) ? key : $"custom_{key}",
            string.IsNullOrWhiteSpace(rule.Category) ? "customRules" : rule.Category.Trim(),
            rule.Content.Trim(),
            rule.InputLabel.Trim(),
            valueType,
            NormalizeValue(rule.Value, valueType),
            rule.Enabled,
            false,
            false,
            rule.SortOrder == 0 ? 10_000 + index : rule.SortOrder);
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
