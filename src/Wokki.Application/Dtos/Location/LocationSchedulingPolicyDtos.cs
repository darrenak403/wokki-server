using System.Text.Json;

namespace Wokki.Application.Dtos.Location;

public sealed record LocationSchedulingPolicyResponse(
    Guid LocationId,
    string SchemaVersion,
    IReadOnlyList<LocationSchedulingRuleDto> Rules,
    DateTime UpdatedAt);

public sealed record UpsertLocationSchedulingPolicyRequest(
    IReadOnlyList<LocationSchedulingRuleUpsertDto> Rules,
    string SchemaVersion = "location-scheduling-policy.v3");

public sealed record LocationSchedulingRuleDto(
    string Key,
    string Category,
    string Content,
    string InputLabel,
    string ValueType,
    JsonElement? Value,
    bool Enabled,
    bool IsDefault,
    bool IsRequired,
    int SortOrder);

public sealed record LocationSchedulingRuleUpsertDto(
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
