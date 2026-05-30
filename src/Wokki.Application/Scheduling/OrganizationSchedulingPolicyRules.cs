using System.Text.Json;
using Wokki.Application.Dtos.Scheduling;
using Wokki.Domain.Entities;

namespace Wokki.Application.Scheduling;

public static class OrganizationSchedulingPolicyRules
{
    public const string SchemaVersion = SchedulingRuleCatalog.SchemaVersion;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<SchedulingRuleDto> GetDefaultRules() => SchedulingRuleCatalog.GetDefaultRuleDtos();

    public static IReadOnlyList<SchedulingRuleDto> GetEffectiveRules(OrganizationSchedulingPolicy? policy)
    {
        if (policy is null)
            return GetDefaultRules();

        var stored = Deserialize(policy.RulesJson).ToList();
        var storedByKey = stored.ToDictionary(rule => rule.Key, StringComparer.OrdinalIgnoreCase);
        var catalogKeys = SchedulingRuleCatalog.EnforcedByKey.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mergedEnforced = SchedulingRuleCatalog.EnforcedRules
            .Select(definition =>
            {
                if (!storedByKey.TryGetValue(definition.Key, out var saved))
                    return SchedulingRuleCatalog.ToRuleDto(definition);

                return SchedulingRuleCatalog.ToRuleDto(
                    definition,
                    saved.Value ?? definition.DefaultValue,
                    saved.Enabled);
            })
            .ToList();

        var advisoryRules = stored
            .Where(rule => !catalogKeys.Contains(rule.Key))
            .Select(rule => rule with
            {
                Category = string.IsNullOrWhiteSpace(rule.Category) ? "customRules" : rule.Category,
                IsDefault = false,
                Enforcement = "advisory"
            })
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.InputLabel)
            .ToList();

        return mergedEnforced.Concat(advisoryRules).ToList();
    }

    public static string Serialize(IReadOnlyList<SchedulingRuleDto> rules) =>
        JsonSerializer.Serialize(rules, JsonOptions);

    public static string SerializeUpsert(IReadOnlyList<SchedulingRuleUpsertDto> rules) =>
        Serialize(NormalizeUpsert(rules));

    public static IReadOnlyList<SchedulingRuleDto> NormalizeUpsert(IReadOnlyList<SchedulingRuleUpsertDto> rules)
    {
        var catalogByKey = SchedulingRuleCatalog.EnforcedByKey;
        var incomingByKey = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Key))
            .GroupBy(rule => NormalizeKey(rule.Key!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var mergedEnforced = SchedulingRuleCatalog.EnforcedRules
            .Select(definition =>
            {
                if (!incomingByKey.TryGetValue(definition.Key, out var incoming))
                    return SchedulingRuleCatalog.ToRuleDto(definition);

                var valueType = NormalizeValueType(incoming.ValueType, definition.ValueType);
                return SchedulingRuleCatalog.ToRuleDto(
                    definition,
                    NormalizeValue(incoming.Value, valueType),
                    incoming.Enabled);
            })
            .ToList();

        var advisoryRules = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Key)
                && !catalogByKey.ContainsKey(NormalizeKey(rule.Key!)))
            .Select((rule, index) => NormalizeAdvisoryRule(rule, index))
            .Where(rule => rule is not null)
            .Cast<SchedulingRuleDto>()
            .ToList();

        return mergedEnforced
            .Concat(advisoryRules)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.InputLabel)
            .ToList();
    }

    public static bool TryValidate(IReadOnlyList<SchedulingRuleUpsertDto> rules)
    {
        if (rules.Count > 100)
            return false;

        var normalized = NormalizeUpsert(rules);
        var advisoryCount = normalized.Count(rule => rule.Enforcement == "advisory");
        if (advisoryCount > SchedulingRuleCatalog.MaxAdvisoryRules)
            return false;

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in normalized)
        {
            if (!keys.Add(rule.Key)
                || !IsSupportedValueType(rule.ValueType)
                || !ValueMatchesType(rule.Value, rule.ValueType))
                return false;

            if (rule.Enforcement == "advisory"
                && (string.IsNullOrWhiteSpace(rule.Content) || string.IsNullOrWhiteSpace(rule.InputLabel)))
                return false;

            if (rule.IsRequired && rule.ValueType == "number" && rule.Value is null)
                return false;
        }

        return true;
    }

    public static int GetInt(OrganizationSchedulingPolicy? policy, string key, int fallback)
    {
        var rule = FindRule(policy, key, requireEnabled: false);
        return rule is null || !rule.Enabled ? fallback : ReadIntValue(rule.Value, fallback);
    }

    public static bool GetBool(OrganizationSchedulingPolicy? policy, string key, bool fallback)
    {
        var rule = FindRule(policy, key, requireEnabled: false);
        return rule is null || !rule.Enabled ? false : ReadBoolValue(rule.Value, fallback);
    }

    public static int ReadIntValue(JsonElement? value, int fallback) => ReadInt(value, fallback);

    public static bool ReadBoolValue(JsonElement? value, bool fallback) => ReadBool(value, fallback);

    private static SchedulingRuleDto? FindRule(
        OrganizationSchedulingPolicy? policy,
        string key,
        bool requireEnabled = true)
    {
        return GetEffectiveRules(policy)
            .FirstOrDefault(item =>
                (!requireEnabled || item.Enabled)
                && item.Enforcement == "enforced"
                && item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static SchedulingRuleDto? NormalizeAdvisoryRule(SchedulingRuleUpsertDto rule, int index)
    {
        if (string.IsNullOrWhiteSpace(rule.Key))
            return null;

        var key = NormalizeKey(rule.Key);
        if (SchedulingRuleCatalog.EnforcedByKey.ContainsKey(key))
            return null;

        if (!string.Equals(rule.Category, "customRules", StringComparison.OrdinalIgnoreCase))
            return null;

        var valueType = NormalizeValueType(rule.ValueType, "text");
        var normalizedKey = key.StartsWith("custom_", StringComparison.Ordinal) ? key : $"custom_{key}";

        return new SchedulingRuleDto(
            normalizedKey,
            "customRules",
            rule.Content.Trim(),
            rule.InputLabel.Trim(),
            valueType,
            NormalizeValue(rule.Value, valueType),
            rule.Enabled,
            false,
            false,
            rule.SortOrder == 0 ? 10_000 + index : rule.SortOrder,
            "advisory");
    }

    private static IReadOnlyList<SchedulingRuleDto> Deserialize(string rulesJson)
    {
        if (string.IsNullOrWhiteSpace(rulesJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<SchedulingRuleDto>>(rulesJson, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

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
            "number" => JsonSerializer.SerializeToElement(ReadInt(value, 0), JsonOptions),
            "boolean" => JsonSerializer.SerializeToElement(ReadBool(value, false), JsonOptions),
            _ => JsonSerializer.SerializeToElement(
                value.Value.ValueKind == JsonValueKind.String
                    ? value.Value.GetString() ?? string.Empty
                    : value.Value.ToString(),
                JsonOptions)
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
