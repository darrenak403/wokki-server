using Wokki.Application.Scheduling;
using Wokki.Common.Utils;
using Wokki.Domain.Entities;
using Wokki.Domain.Repositories;
using EmployeeEntity = Wokki.Domain.Entities.Employee;
using ScheduleEntity = Wokki.Domain.Entities.Schedule;

namespace Wokki.Application.Services.SwapPost.Implementations;

public sealed class SwapPostPolicyValidator(IUnitOfWork unitOfWork)
{
    public async Task<AppMessage?> ValidatePostSwapStateAsync(
        EmployeeEntity employee,
        ShiftAssignment assignmentAfterSwap,
        ShiftDefinition shift,
        ScheduleEntity schedule,
        IReadOnlyList<ShiftAssignment> scheduleAssignments,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        IReadOnlySet<Guid> ignoredAssignmentIds,
        OrganizationSchedulingSolverPolicy policy,
        CancellationToken cancellationToken)
    {
        if (policy.RequireRoleMatch && !SchedulingRoleMatcher.Matches(employee, shift))
            return AppMessages.SwapPost.PolicyRoleMismatch;

        if (await HasDuplicateSlotAsync(
                schedule.Id,
                employee.Id,
                assignmentAfterSwap.ShiftDefinitionId,
                assignmentAfterSwap.Date,
                ignoredAssignmentIds,
                scheduleAssignments,
                cancellationToken))
            return AppMessages.SwapPost.PolicyOverlap;

        if (HasSameDayOverlap(
                employee.Id,
                assignmentAfterSwap.Date,
                shift,
                scheduleAssignments,
                shiftsById,
                ignoredAssignmentIds))
            return AppMessages.SwapPost.PolicyOverlap;

        if (policy.MinRestMinutesEnabled
            && SchedulingShiftConflictRules.ConflictsWithRestPolicy(
                employee.Id,
                assignmentAfterSwap.Date,
                shift,
                scheduleAssignments,
                shiftsById,
                ignoredAssignmentIds,
                policy.MinRestMinutesBetweenShifts))
            return AppMessages.SwapPost.PolicyRestConflict;

        var planned = BuildPlannedAssignments(scheduleAssignments, ignoredAssignmentIds, assignmentAfterSwap);
        if (policy.MaxShiftsPerDayEnabled
            && planned.Count(a => a.EmployeeId == employee.Id && a.Date == assignmentAfterSwap.Date)
                > policy.MaxShiftsPerEmployeePerDay)
            return AppMessages.SwapPost.PolicyDailyCap;

        if (policy.MaxShiftsPerWeekEnabled
            && planned.Count(a => a.EmployeeId == employee.Id) > policy.MaxShiftsPerEmployeePerWeek)
            return AppMessages.SwapPost.PolicyWeeklyCap;

        return null;
    }

    private static List<ShiftAssignment> BuildPlannedAssignments(
        IReadOnlyList<ShiftAssignment> scheduleAssignments,
        IReadOnlySet<Guid> ignoredAssignmentIds,
        ShiftAssignment assignmentAfterSwap)
    {
        var planned = scheduleAssignments
            .Where(a => !ignoredAssignmentIds.Contains(a.Id))
            .Select(a => new ShiftAssignment
            {
                Id = a.Id,
                OrganizationId = a.OrganizationId,
                ScheduleId = a.ScheduleId,
                ShiftDefinitionId = a.ShiftDefinitionId,
                EmployeeId = a.EmployeeId,
                Date = a.Date,
                Note = a.Note,
                CreatedAt = a.CreatedAt
            })
            .ToList();

        var index = planned.FindIndex(a => a.Id == assignmentAfterSwap.Id);
        if (index >= 0)
            planned[index] = assignmentAfterSwap;
        else
            planned.Add(assignmentAfterSwap);

        return planned;
    }

    private static bool HasSameDayOverlap(
        Guid employeeId,
        DateOnly date,
        ShiftDefinition candidateShift,
        IReadOnlyList<ShiftAssignment> scheduleAssignments,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftsById,
        IReadOnlySet<Guid> ignoredAssignmentIds)
    {
        foreach (var assignment in scheduleAssignments)
        {
            if (assignment.EmployeeId != employeeId
                || assignment.Date != date
                || ignoredAssignmentIds.Contains(assignment.Id))
                continue;

            if (!shiftsById.TryGetValue(assignment.ShiftDefinitionId, out var existingShift))
                continue;

            if (SchedulingShiftConflictRules.HasTimeOverlap(
                    candidateShift.StartTime,
                    candidateShift.EndTime,
                    existingShift.StartTime,
                    existingShift.EndTime))
                return true;
        }

        return false;
    }

    private async Task<bool> HasDuplicateSlotAsync(
        Guid scheduleId,
        Guid employeeId,
        Guid shiftDefinitionId,
        DateOnly date,
        IReadOnlySet<Guid> ignoredAssignmentIds,
        IReadOnlyList<ShiftAssignment> scheduleAssignments,
        CancellationToken cancellationToken)
    {
        if (scheduleAssignments.Any(a =>
                !ignoredAssignmentIds.Contains(a.Id)
                && a.EmployeeId == employeeId
                && a.ShiftDefinitionId == shiftDefinitionId
                && a.Date == date))
            return true;

        return await unitOfWork.ShiftAssignments.ExistsAsync(
            scheduleId,
            shiftDefinitionId,
            employeeId,
            date,
            cancellationToken);
    }
}
