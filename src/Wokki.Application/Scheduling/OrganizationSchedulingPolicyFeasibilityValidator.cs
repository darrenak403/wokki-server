using Wokki.Application.Dtos.Scheduling;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;

namespace Wokki.Application.Scheduling;

public sealed class OrganizationSchedulingPolicyFeasibilityValidator(IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<string>> ValidateAsync(
        Guid organizationId,
        IReadOnlyList<SchedulingRuleDto> rules,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var solverPolicy = OrganizationSchedulingSolverPolicy.FromEffectiveRules(rules);

        if (solverPolicy.RequireFullCoverage && !solverPolicy.MinStaffPerShiftEnabled)
        {
            errors.Add("Bật \"Số người tối thiểu / ca\" khi dùng \"Yêu cầu phủ đủ ca\".");
        }

        if (solverPolicy.MinStaffPerShiftEnabled && solverPolicy.MaxStaffPerShiftEnabled
            && solverPolicy.MinStaffPerShift > solverPolicy.MaxStaffPerShift)
        {
            errors.Add(
                $"Số người tối thiểu / ca ({solverPolicy.MinStaffPerShift}) không được lớn hơn số người tối đa / ca ({solverPolicy.MaxStaffPerShift}).");
        }

        if (solverPolicy.MinStaffPerShiftEnabled && solverPolicy.MinStaffPerShift > 0)
        {
            var employeeCount = await CountActiveEmployeesAsync(organizationId, cancellationToken);
            if (employeeCount > 0 && solverPolicy.MinStaffPerShift > employeeCount)
            {
                errors.Add(
                    $"Số người tối thiểu / ca ({solverPolicy.MinStaffPerShift}) lớn hơn số nhân viên active ({employeeCount}).");
            }
        }

        if (solverPolicy.MinRestMinutesEnabled && solverPolicy.MinRestMinutesBetweenShifts > 0)
        {
            var minGap = await GetMinSameDayShiftGapMinutesAsync(organizationId, cancellationToken);
            if (minGap.HasValue && solverPolicy.MinRestMinutesBetweenShifts > minGap.Value)
            {
                errors.Add(
                    $"Nghỉ giữa ca ({solverPolicy.MinRestMinutesBetweenShifts} phút) lớn hơn khoảng cách nhỏ nhất giữa hai ca cùng ngày ({minGap.Value} phút).");
            }
        }

        return errors;
    }

    private async Task<int> CountActiveEmployeesAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var page = await unitOfWork.Employees.ListAsync(
            1,
            1,
            organizationId,
            departmentId: null,
            locationIds: null,
            cancellationToken: cancellationToken);
        return page.TotalCount;
    }

    private async Task<int?> GetMinSameDayShiftGapMinutesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var locations = await unitOfWork.Locations.ListAsync(organizationId, cancellationToken: cancellationToken);
        double? minGap = null;

        foreach (var location in locations)
        {
            var shifts = await unitOfWork.ShiftDefinitions.ListAsync(
                location.Id,
                departmentId: null,
                activeOnly: true,
                cancellationToken);

            for (var i = 0; i < shifts.Count; i++)
            for (var j = i + 1; j < shifts.Count; j++)
            {
                var gap = GetGapMinutes(shifts[i], shifts[j]);
                minGap = minGap.HasValue ? Math.Min(minGap.Value, gap) : gap;
            }
        }

        return minGap.HasValue ? (int)Math.Floor(minGap.Value) : null;
    }

    private static double GetGapMinutes(ShiftDefinition left, ShiftDefinition right)
    {
        var aStart = left.StartTime.ToTimeSpan();
        var aEnd = left.EndTime.ToTimeSpan();
        var bStart = right.StartTime.ToTimeSpan();
        var bEnd = right.EndTime.ToTimeSpan();

        if (aEnd <= aStart) aEnd = aEnd.Add(TimeSpan.FromDays(1));
        if (bEnd <= bStart) bEnd = bEnd.Add(TimeSpan.FromDays(1));

        var firstStart = aStart <= bStart ? aStart : bStart;
        var firstEnd = aStart <= bStart ? aEnd : bEnd;
        var secondStart = aStart <= bStart ? bStart : aStart;

        return (secondStart - firstEnd).TotalMinutes;
    }
}
