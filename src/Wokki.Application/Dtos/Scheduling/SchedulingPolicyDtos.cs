using System.Text.Json;

namespace Wokki.Application.Dtos.Scheduling;

public sealed record SchedulingRuleCatalogResponse(
    string SchemaVersion,
    IReadOnlyList<SchedulingRuleCatalogCategoryDto> Categories,
    IReadOnlyList<SchedulingRuleCatalogEntryDto> Rules);

public sealed record SchedulingRuleCatalogCategoryDto(
    string Id,
    string Label,
    string? Hint);

public sealed record SchedulingRuleCatalogEntryDto(
    string Key,
    string Category,
    string Content,
    string InputLabel,
    string ValueType,
    JsonElement? DefaultValue,
    bool IsRequired,
    int SortOrder,
    string Enforcement);

public sealed record OrganizationSchedulingPolicyResponse(
    Guid OrganizationId,
    string SchemaVersion,
    IReadOnlyList<SchedulingRuleDto> Rules,
    DateTime UpdatedAt);

public sealed record UpsertOrganizationSchedulingPolicyRequest(
    IReadOnlyList<SchedulingRuleUpsertDto> Rules,
    string SchemaVersion = "org-scheduling-policy.v1");

public sealed record SchedulingRuleDto(
    string Key,
    string Category,
    string Content,
    string InputLabel,
    string ValueType,
    JsonElement? Value,
    bool Enabled,
    bool IsDefault,
    bool IsRequired,
    int SortOrder,
    string Enforcement);

public sealed record SchedulingRuleUpsertDto(
    string? Key,
    string Category,
    string Content,
    string InputLabel,
    string ValueType,
    JsonElement? Value,
    bool Enabled = true,
    bool IsDefault = false,
    bool IsRequired = false,
    int SortOrder = 0);
