using System.Text;
using System.Text.Json;
using Wokki.Application.Dtos.Bedrock;
using Wokki.Application.Dtos.Schedule;
using Wokki.Application.Scheduling;
using Wokki.Application.Services.Bedrock.Interfaces;
using Wokki.Application.Services.Schedule.Interfaces;
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
    IBedrockService bedrockService) : IScheduleInsightService
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
        var payload = BuildPayload(
            schedule,
            department,
            employees,
            shifts,
            existingAssignments,
            submittedPreferences,
            suggestions,
            request);
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
                new ScheduleInsightChatResponse(scheduleId, refused, "local-guard", context.GeneratedAt),
                AppMessages.ScheduleInsight.ChatAnswered);
        }

        try
        {
            var prompt = BuildPrompt(context.JsonContent, request.Question);
            var result = await bedrockService.ConverseAsync(
                prompt,
                new BedrockConverseOptions(MaxTokens: 1200, Temperature: 0.2f, TimeoutSeconds: 30),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(result.Text))
                return ApiResponse<ScheduleInsightChatResponse>.FailureResponse(AppMessages.ScheduleInsight.ChatUnavailable);

            return ApiResponse<ScheduleInsightChatResponse>.SuccessResponse(
                new ScheduleInsightChatResponse(scheduleId, result.Text.Trim(), "bedrock", context.GeneratedAt),
                AppMessages.ScheduleInsight.ChatAnswered);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResponse<ScheduleInsightChatResponse>.FailureResponse(AppMessages.ScheduleInsight.ChatUnavailable);
        }
        catch
        {
            return ApiResponse<ScheduleInsightChatResponse>.FailureResponse(AppMessages.ScheduleInsight.ChatUnavailable);
        }
    }

    private static object BuildPayload(
        ScheduleEntity schedule,
        DepartmentEntity department,
        IReadOnlyList<EmployeeEntity> employees,
        IReadOnlyList<ShiftDefinition> shifts,
        IReadOnlyList<ShiftAssignment> existingAssignments,
        IReadOnlyList<SchedulePreferenceSubmission> submittedPreferences,
        IReadOnlyList<ScheduleInsightSuggestionInput> suggestions,
        GenerateScheduleInsightContextRequest request)
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

        var maxWeekly = SchedulingSolverDefaults.MaxShiftsPerEmployeePerWeek;
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
                NearMaxCap = loads.Where(x => x.ShiftCount >= maxWeekly - 1).ToList()
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
