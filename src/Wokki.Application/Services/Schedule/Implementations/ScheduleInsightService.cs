using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Wokki.Application.Dtos.Bedrock;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Dtos.Scheduling;
using Wokki.Application.Scheduling;
using Wokki.Application.Services.Bedrock.Interfaces;
using Wokki.Application.Services.Schedule.Interfaces;
using Wokki.Application.Validators.Scheduling;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Enums;
using Wokki.Domain.Repositories;
using DepartmentEntity = Wokki.Domain.Entities.Department;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using ScheduleEntity = Wokki.Domain.Entities.Schedule;

namespace Wokki.Application.Services.Schedule.Implementations;

public sealed class ScheduleInsightService(
    IUnitOfWork unitOfWork,
    IBedrockService bedrockService,
    ILogger<ScheduleInsightService> logger) : IScheduleInsightService
{
    private const string SchemaVersion = "1.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<ApiResponse<ScheduleInsightContextResponse>> GenerateContextAsync(
        Guid scheduleId,
        GenerateScheduleInsightContextRequest request,
        CancellationToken cancellationToken = default)
    {
        var schedule = await unitOfWork.Schedules.GetByIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (schedule is null)
            return ApiResponse<ScheduleInsightContextResponse>.FailureResponse(AppMessages.Schedule.NotFound);

        var department = await unitOfWork.Departments.GetByIdAsync(schedule.DepartmentId, cancellationToken: cancellationToken);
        if (department is null)
            return ApiResponse<ScheduleInsightContextResponse>.FailureResponse(AppMessages.Schedule.DepartmentNotFound);

        var employeePage = await unitOfWork.Employees.ListAsync(
            1,
            500,
            department.OrganizationId,
            schedule.DepartmentId,
            locationIds: new HashSet<Guid> { department.LocationId },
            cancellationToken: cancellationToken);
        var employees = employeePage.Items.Where(e => e.TerminatedAt is null).ToList();
        var employeeMap = employees.ToDictionary(e => e.Id);

        var shifts = await unitOfWork.ShiftDefinitions.ListAsync(
            department.LocationId,
            schedule.DepartmentId,
            activeOnly: true,
            cancellationToken);
        var shiftMap = shifts.ToDictionary(s => s.Id);

        var existingAssignments = await unitOfWork.ShiftAssignments.ListByScheduleAsync(scheduleId, cancellationToken);
        var submittedPreferences = await unitOfWork.SchedulePreferences.ListByScheduleAsync(
            scheduleId,
            includeLines: true,
            status: SchedulePreferenceStatus.Submitted,
            cancellationToken);
        var suggestions = request.Suggestions ?? [];
        var orgPolicy = await unitOfWork.OrganizationSchedulingPolicies.GetByOrganizationIdAsync(
            department.OrganizationId,
            cancellationToken: cancellationToken);
        var solverPolicy = OrganizationSchedulingSolverPolicy.FromOrgPolicy(orgPolicy);
        var payload = BuildPayload(
            schedule,
            department,
            employees,
            shifts,
            existingAssignments,
            submittedPreferences,
            suggestions,
            request,
            solverPolicy);
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        var now = DateTime.UtcNow;
        var entity = await unitOfWork.ScheduleInsightContexts.GetByScheduleIdAsync(
            scheduleId,
            track: true,
            cancellationToken);
        if (entity is null)
        {
            entity = new ScheduleInsightContext
            {
                OrganizationId = department.OrganizationId,
                ScheduleId = scheduleId,
                LocationId = department.LocationId,
                DepartmentId = schedule.DepartmentId,
                WeekStartDate = schedule.WeekStartDate,
                SchemaVersion = SchemaVersion,
                Provider = NormalizeProvider(request.Provider),
                FallbackUsed = request.FallbackUsed,
                JsonContent = json,
                GeneratedAt = now,
                UpdatedAt = now,
                ExpiresAt = schedule.WeekStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(14)
            };
            await unitOfWork.ScheduleInsightContexts.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.OrganizationId = department.OrganizationId;
            entity.SchemaVersion = SchemaVersion;
            entity.LocationId = department.LocationId;
            entity.DepartmentId = schedule.DepartmentId;
            entity.WeekStartDate = schedule.WeekStartDate;
            entity.Provider = NormalizeProvider(request.Provider);
            entity.FallbackUsed = request.FallbackUsed;
            entity.JsonContent = json;
            entity.GeneratedAt = now;
            entity.UpdatedAt = now;
            entity.ExpiresAt = schedule.WeekStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(14);
            unitOfWork.ScheduleInsightContexts.Update(entity);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ScheduleInsightContextResponse>.SuccessResponse(
            ToResponse(entity),
            AppMessages.ScheduleInsight.ContextGenerated);
    }

    public async Task<ApiResponse<ScheduleInsightContextResponse>> GetContextAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var entity = await unitOfWork.ScheduleInsightContexts.GetByScheduleIdAsync(scheduleId, cancellationToken: cancellationToken);
        if (entity is null)
            return ApiResponse<ScheduleInsightContextResponse>.FailureResponse(AppMessages.ScheduleInsight.ContextNotFound);

        return ApiResponse<ScheduleInsightContextResponse>.SuccessResponse(
            ToResponse(entity),
            AppMessages.ScheduleInsight.ContextFound);
    }

    public async Task<ApiResponse<ScheduleInsightChatResponse>> ChatAsync(
        Guid scheduleId,
        ScheduleInsightChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = await unitOfWork.ScheduleInsightContexts.GetByScheduleIdAsync(
            scheduleId,
            cancellationToken: cancellationToken);
        if (context is null)
            return ApiResponse<ScheduleInsightChatResponse>.FailureResponse(AppMessages.ScheduleInsight.ContextNotFound);

        if (context.ExpiresAt <= DateTime.UtcNow)
            return ApiResponse<ScheduleInsightChatResponse>.FailureResponse(AppMessages.ScheduleInsight.ContextExpired);

        if (RequestsMutation(request.Question))
        {
            var refused = "Tôi chỉ hỗ trợ giải thích và gợi ý dựa trên snapshot lịch tuần này. Tôi không thể áp dụng, ghi, cập nhật hoặc thay đổi phân công; Manager/Admin cần thao tác trong màn hình lịch.";
            return ApiResponse<ScheduleInsightChatResponse>.SuccessResponse(
                new ScheduleInsightChatResponse(scheduleId, refused, "local-guard", context.GeneratedAt, request.HintMode),
                AppMessages.ScheduleInsight.ChatAnswered);
        }

        string bedrockText;
        try
        {
            var prompt = request.HintMode
                ? BuildHintPrompt(context.JsonContent, request.Question)
                : BuildPrompt(context.JsonContent, request.Question);
            var options = request.HintMode
                ? new BedrockConverseOptions(MaxTokens: 600, Temperature: 0f, TimeoutSeconds: 30)
                : new BedrockConverseOptions(MaxTokens: 1200, Temperature: 0.2f, TimeoutSeconds: 30);
            var result = await bedrockService.ConverseAsync(prompt, options, cancellationToken);

            if (string.IsNullOrWhiteSpace(result.Text))
                return ApiResponse<ScheduleInsightChatResponse>.FailureResponse(AppMessages.ScheduleInsight.ChatUnavailable);

            bedrockText = result.Text.Trim();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Bedrock chat timed out for schedule {ScheduleId}", scheduleId);
            return ApiResponse<ScheduleInsightChatResponse>.FailureResponse(AppMessages.ScheduleInsight.ChatUnavailable);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bedrock chat failed for schedule {ScheduleId}: {ExType} — {Message}",
                scheduleId, ex.GetType().Name, ex.Message);
            return ApiResponse<ScheduleInsightChatResponse>.FailureResponse(AppMessages.ScheduleInsight.ChatUnavailable);
        }

        if (!request.HintMode)
        {
            return ApiResponse<ScheduleInsightChatResponse>.SuccessResponse(
                new ScheduleInsightChatResponse(scheduleId, bedrockText, "bedrock", context.GeneratedAt),
                AppMessages.ScheduleInsight.ChatAnswered);
        }

        // Hint-mode parsing/validation runs outside the Bedrock try/catch above: a successfully
        // returned response that fails to parse/validate is "could not understand," never the
        // same outcome as a Bedrock-unavailable failure (FR-02).
        var parsedHint = TryParseHint(bedrockText);
        if (parsedHint is null)
            return ApiResponse<ScheduleInsightChatResponse>.SuccessResponse(
                NotUnderstoodResponse(scheduleId, context.GeneratedAt),
                AppMessages.ScheduleInsight.HintNotUnderstood);

        var roster = await LoadActiveRosterAsync(context, cancellationToken);
        var validation = ScheduleSuggestionHintValidator.Validate(parsedHint, roster);
        if (!validation.IsValid)
            return ApiResponse<ScheduleInsightChatResponse>.SuccessResponse(
                NotUnderstoodResponse(scheduleId, context.GeneratedAt),
                AppMessages.ScheduleInsight.HintNotUnderstood);

        var summary = BuildHintSummary(parsedHint, roster);
        return ApiResponse<ScheduleInsightChatResponse>.SuccessResponse(
            new ScheduleInsightChatResponse(
                scheduleId,
                summary,
                "bedrock",
                context.GeneratedAt,
                HintMode: true,
                Hint: parsedHint,
                HintSummary: summary,
                HintUnderstood: true),
            AppMessages.ScheduleInsight.HintGenerated);
    }

    private async Task<IReadOnlyList<EmployeeEntity>> LoadActiveRosterAsync(
        ScheduleInsightContext context,
        CancellationToken cancellationToken)
    {
        var employeePage = await unitOfWork.Employees.ListAsync(
            1,
            500,
            context.OrganizationId,
            context.DepartmentId,
            locationIds: new HashSet<Guid> { context.LocationId },
            cancellationToken: cancellationToken);
        return employeePage.Items.Where(e => e.TerminatedAt is null).ToList();
    }

    private const string HintNotUnderstoodMessageVi =
        "Không hiểu được yêu cầu này là gợi ý xếp ca. Thử mô tả ngắn gọn hơn, ví dụ: ưu tiên nhóm nào, hoặc giới hạn bao nhiêu ca.";

    private static ScheduleInsightChatResponse NotUnderstoodResponse(Guid scheduleId, DateTime contextGeneratedAt) =>
        new(scheduleId, HintNotUnderstoodMessageVi, "bedrock", contextGeneratedAt, HintMode: true);

    private static readonly JsonSerializerOptions HintParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static ScheduleSuggestionHint? TryParseHint(string rawText)
    {
        var json = ExtractJsonObject(rawText);
        if (json is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<ScheduleSuggestionHint>(json, HintParseOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start < 0 || end <= start ? null : text[start..(end + 1)];
    }

    private static string BuildHintSummary(ScheduleSuggestionHint hint, IReadOnlyList<EmployeeEntity> roster) =>
        hint.Kind switch
        {
            ScheduleSuggestionHintKind.PreferenceWeight =>
                $"{(hint.Direction == HintWeightDirection.Boost ? "Ưu tiên" : "Giảm ưu tiên")} {DescribeGroup(hint.Group, roster)} trong lần Suggest tiếp theo (không lưu vào chính sách lịch của tổ chức).",
            ScheduleSuggestionHintKind.TempMinMax =>
                $"Đặt {DescribeMinMax(hint)} cho {DescribeGroup(hint.Group, roster)} chỉ trong lần Suggest tiếp theo (không lưu vào chính sách lịch của tổ chức).",
            ScheduleSuggestionHintKind.AvoidPairing =>
                $"Tránh xếp {EmployeeDisplayName(hint.EmployeeId1, roster)} và {EmployeeDisplayName(hint.EmployeeId2, roster)} cùng ca trong lần Suggest tiếp theo (không lưu vào chính sách lịch của tổ chức).",
            _ => "Không rõ loại gợi ý."
        };

    private static string DescribeMinMax(ScheduleSuggestionHint hint)
    {
        var parts = new List<string>();
        if (hint.MinCount.HasValue)
            parts.Add($"tối thiểu {hint.MinCount} người");
        if (hint.MaxCount.HasValue)
            parts.Add($"tối đa {hint.MaxCount} người");
        return string.Join(" và ", parts);
    }

    private static string DescribeGroup(HintGroupRef? group, IReadOnlyList<EmployeeEntity> roster) =>
        group?.GroupType switch
        {
            HintGroupType.RoleDepartment => $"nhân viên có vai trò \"{group!.Role}\"",
            HintGroupType.ExplicitEmployeeIds => string.Join(
                ", ",
                (group!.EmployeeIds ?? []).Select(id => EmployeeDisplayName(id, roster))),
            _ => "nhóm không xác định"
        };

    private static string EmployeeDisplayName(Guid? id, IReadOnlyList<EmployeeEntity> roster) =>
        id is null
            ? "?"
            : roster.FirstOrDefault(e => e.Id == id) is { } employee
                ? $"{employee.FirstName} {employee.LastName}".Trim()
                : id.ToString()!;

    private static string BuildHintPrompt(string contextJson, string question)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Bạn là bộ phân loại yêu cầu xếp ca cho Wokki. Chỉ trả về DUY NHẤT một JSON object, không markdown, không chữ giải thích.");
        prompt.AppendLine("Chỉ có đúng 3 loại hint hợp lệ, dùng field \"kind\":");
        prompt.AppendLine("1. {\"kind\":\"PreferenceWeight\",\"group\":<group>,\"direction\":\"Boost\"|\"Reduce\"}");
        prompt.AppendLine("2. {\"kind\":\"TempMinMax\",\"group\":<group>,\"minCount\":number|null,\"maxCount\":number|null}");
        prompt.AppendLine("3. {\"kind\":\"AvoidPairing\",\"employeeId1\":\"<id>\",\"employeeId2\":\"<id>\"}");
        prompt.AppendLine("<group> là {\"groupType\":\"RoleDepartment\",\"role\":\"<Position lấy đúng từ Employees trong context>\"} hoặc {\"groupType\":\"ExplicitEmployeeIds\",\"employeeIds\":[\"<id>\"]}.");
        prompt.AppendLine("employeeIds/employeeId1/employeeId2 PHẢI là Employees[].Id có thật trong context JSON bên dưới, không tự tạo id.");
        prompt.AppendLine("Nếu câu hỏi của Manager không thể quy về đúng 1 trong 3 dạng trên, trả về {\"kind\":\"Unknown\"}.");
        prompt.AppendLine();
        prompt.AppendLine("Schedule context JSON:");
        prompt.AppendLine(contextJson);
        prompt.AppendLine();
        prompt.AppendLine("Câu hỏi của Manager:");
        prompt.AppendLine(question);
        return prompt.ToString();
    }

    private static object BuildPayload(
        ScheduleEntity schedule,
        DepartmentEntity department,
        IReadOnlyList<EmployeeEntity> employees,
        IReadOnlyList<ShiftDefinition> shifts,
        IReadOnlyList<ShiftAssignment> existingAssignments,
        IReadOnlyList<SchedulePreferenceSubmission> submittedPreferences,
        IReadOnlyList<ScheduleInsightSuggestionInput> suggestions,
        GenerateScheduleInsightContextRequest request,
        OrganizationSchedulingSolverPolicy solverPolicy)
    {
        var employeeMap = employees.ToDictionary(e => e.Id);
        var shiftMap = shifts.ToDictionary(s => s.Id);
        var preferenceLines = submittedPreferences
            .SelectMany(s => s.Lines.Select(l => new
            {
                s.EmployeeId,
                l.ShiftDefinitionId,
                l.Date,
                l.PreferenceType
            }))
            .ToList();

        var planned = existingAssignments
            .Select(a => new
            {
                a.ShiftDefinitionId,
                a.EmployeeId,
                a.Date,
                Source = "existing",
                Score = (int?)null,
                Explanations = Array.Empty<string>(),
                Warnings = Array.Empty<string>()
            })
            .Concat(suggestions.Select(s => new
            {
                s.ShiftDefinitionId,
                s.EmployeeId,
                s.Date,
                Source = "suggested",
                Score = (int?)s.Score,
                Explanations = s.Explanations?.ToArray() ?? [],
                Warnings = s.Warnings?.ToArray() ?? []
            }))
            .ToList();

        var loads = planned
            .GroupBy(a => a.EmployeeId)
            .Select(g => new
            {
                EmployeeId = g.Key,
                EmployeeName = employeeMap.TryGetValue(g.Key, out var employee)
                    ? $"{employee.FirstName} {employee.LastName}".Trim()
                    : string.Empty,
                ShiftCount = g.Count()
            })
            .OrderBy(x => x.EmployeeName)
            .ToList();

        var maxWeekly = solverPolicy.MaxShiftsPerWeekEnabled
            ? solverPolicy.MaxShiftsPerEmployeePerWeek
            : (int?)null;
        var minLoad = loads.Count == 0 ? 0 : loads.Min(x => x.ShiftCount);
        var maxLoad = loads.Count == 0 ? 0 : loads.Max(x => x.ShiftCount);
        var averageLoad = loads.Count == 0 ? 0 : loads.Average(x => x.ShiftCount);

        var preferredMatched = suggestions.Count(s => preferenceLines.Any(p =>
            p.EmployeeId == s.EmployeeId
            && p.ShiftDefinitionId == s.ShiftDefinitionId
            && p.Date == s.Date
            && p.PreferenceType == PreferenceType.Preferred));
        var availableUsed = suggestions.Count(s => preferenceLines.Any(p =>
            p.EmployeeId == s.EmployeeId
            && p.ShiftDefinitionId == s.ShiftDefinitionId
            && p.Date == s.Date
            && p.PreferenceType == PreferenceType.Available));
        var unavailableSkipped = preferenceLines.Count(p => p.PreferenceType == PreferenceType.Unavailable
            && !planned.Any(a => a.EmployeeId == p.EmployeeId
                                 && a.ShiftDefinitionId == p.ShiftDefinitionId
                                 && a.Date == p.Date));

        var weekEnd = schedule.WeekStartDate.AddDays(6);
        var coverageRows = new List<object>();
        for (var date = schedule.WeekStartDate; date <= weekEnd; date = date.AddDays(1))
        {
            foreach (var shift in shifts)
            {
                var assigned = planned.Count(a => a.ShiftDefinitionId == shift.Id && a.Date == date);
                coverageRows.Add(new
                {
                    Date = date,
                    ShiftDefinitionId = shift.Id,
                    ShiftName = shift.Name,
                    Assigned = assigned
                });
            }
        }

        return new
        {
            SchemaVersion,
            ScheduleId = schedule.Id,
            schedule.DepartmentId,
            DepartmentName = department.Name,
            schedule.WeekStartDate,
            WeekEndDate = weekEnd,
            ScheduleStatus = schedule.Status.ToString(),
            Rules = new
            {
                RequireSubmittedPreferences = solverPolicy.RequireSubmittedPreferences,
                UnavailableIsHardBlock = solverPolicy.UnavailableIsHardBlock,
                RequireRoleMatch = solverPolicy.RequireRoleMatch,
                RequireFullCoverage = solverPolicy.RequireFullCoverage,
                MinStaffPerShift = solverPolicy.MinStaffPerShiftEnabled ? solverPolicy.MinStaffPerShift : (int?)null,
                MaxStaffPerShift = solverPolicy.MaxStaffPerShiftEnabled ? solverPolicy.MaxStaffPerShift : (int?)null,
                MinRestMinutesBetweenShifts = solverPolicy.MinRestMinutesEnabled ? solverPolicy.MinRestMinutesBetweenShifts : (int?)null,
                MaxShiftsPerEmployeePerDay = solverPolicy.MaxShiftsPerDayEnabled ? solverPolicy.MaxShiftsPerEmployeePerDay : (int?)null,
                MaxShiftsPerEmployeePerWeek = maxWeekly
            },
            Employees = employees.Select(e => new
            {
                e.Id,
                Name = $"{e.FirstName} {e.LastName}".Trim(),
                e.Position
            }),
            Shifts = shifts.Select(s => new
            {
                s.Id,
                s.Name,
                StartTime = s.StartTime.ToString("HH:mm"),
                EndTime = s.EndTime.ToString("HH:mm"),
                s.RequiredRole
            }),
            SubmittedPreferences = preferenceLines.Select(p => new
            {
                p.EmployeeId,
                EmployeeName = employeeMap.TryGetValue(p.EmployeeId, out var employee)
                    ? $"{employee.FirstName} {employee.LastName}".Trim()
                    : string.Empty,
                p.ShiftDefinitionId,
                ShiftName = shiftMap.TryGetValue(p.ShiftDefinitionId, out var shift) ? shift.Name : string.Empty,
                p.Date,
                PreferenceType = p.PreferenceType.ToString()
            }),
            ExistingAssignments = existingAssignments.Select(a => new
            {
                a.ShiftDefinitionId,
                ShiftName = shiftMap.TryGetValue(a.ShiftDefinitionId, out var shift) ? shift.Name : string.Empty,
                a.EmployeeId,
                EmployeeName = employeeMap.TryGetValue(a.EmployeeId, out var employee)
                    ? $"{employee.FirstName} {employee.LastName}".Trim()
                    : string.Empty,
                a.Date
            }),
            SuggestedAssignments = suggestions.Select(s => new
            {
                s.ShiftDefinitionId,
                ShiftName = shiftMap.TryGetValue(s.ShiftDefinitionId, out var shift) ? shift.Name : string.Empty,
                s.EmployeeId,
                EmployeeName = employeeMap.TryGetValue(s.EmployeeId, out var employee)
                    ? $"{employee.FirstName} {employee.LastName}".Trim()
                    : string.Empty,
                s.Date,
                s.Score,
                Explanations = s.Explanations ?? [],
                Warnings = s.Warnings ?? []
            }),
            FairnessSummary = new
            {
                EmployeeLoads = loads,
                MinShifts = minLoad,
                MaxShifts = maxLoad,
                AverageShifts = averageLoad,
                NearMaxCap = loads
                    .Where(x => maxWeekly.HasValue && x.ShiftCount >= maxWeekly.Value - 1)
                    .ToList()
            },
            PreferenceSatisfactionSummary = new
            {
                PreferredMatched = preferredMatched,
                AvailableUsed = availableUsed,
                UnavailableSkipped = unavailableSkipped,
                PreferencesNotSatisfied = preferenceLines.Count(p => p.PreferenceType == PreferenceType.Preferred
                    && !planned.Any(a => a.EmployeeId == p.EmployeeId
                                         && a.ShiftDefinitionId == p.ShiftDefinitionId
                                         && a.Date == p.Date))
            },
            CoverageSummary = new
            {
                Slots = coverageRows
            },
            SolverMetadata = new
            {
                Provider = NormalizeProvider(request.Provider),
                request.SolveStatus,
                request.SolveDurationMs,
                request.FallbackUsed,
                Warnings = request.Warnings ?? [],
                GeneratedAt = DateTime.UtcNow
            }
        };
    }

    private static string BuildPrompt(string contextJson, string question)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Bạn là trợ lý hỗ trợ Manager đọc hiểu lịch làm việc tuần của Wokki.");
        prompt.AppendLine("Chỉ dùng dữ liệu trong schedule context JSON bên dưới. Không bịa nhân viên, ca, luật hoặc phân công không có trong context.");
        prompt.AppendLine("Không được nói rằng bạn đã cập nhật, áp dụng, ghi hoặc thay đổi lịch. Bạn chỉ giải thích, tóm tắt và đưa gợi ý để Manager cân nhắc.");
        prompt.AppendLine("Nếu context thiếu dữ liệu để trả lời, hãy nói rõ là chưa đủ dữ liệu.");
        prompt.AppendLine("Trả lời bằng tiếng Việt, ngắn gọn, có thể dùng bullet khi hữu ích.");
        prompt.AppendLine();
        prompt.AppendLine("Schedule context JSON:");
        prompt.AppendLine(contextJson);
        prompt.AppendLine();
        prompt.AppendLine("Câu hỏi của Manager:");
        prompt.AppendLine(question);
        return prompt.ToString();
    }

    private static bool RequestsMutation(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();
        return normalized.Contains("apply")
               || normalized.Contains("update")
               || normalized.Contains("write")
               || normalized.Contains("save")
               || normalized.Contains("áp dụng")
               || normalized.Contains("cap nhat")
               || normalized.Contains("cập nhật")
               || normalized.Contains("ghi lịch")
               || normalized.Contains("lưu lịch")
               || normalized.Contains("đổi lịch");
    }

    private static string NormalizeProvider(string? provider) =>
        string.IsNullOrWhiteSpace(provider) ? "heuristic" : provider.Trim();

    private static ScheduleInsightContextResponse ToResponse(ScheduleInsightContext entity) =>
        new(
            entity.ScheduleId,
            entity.LocationId,
            entity.DepartmentId,
            entity.WeekStartDate,
            entity.SchemaVersion,
            entity.Provider,
            entity.FallbackUsed,
            entity.GeneratedAt,
            entity.UpdatedAt,
            entity.ExpiresAt,
            entity.JsonContent);
}
