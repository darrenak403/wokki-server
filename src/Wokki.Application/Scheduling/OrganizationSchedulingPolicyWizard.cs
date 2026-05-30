using Wokki.Application.Dtos.Scheduling;

namespace Wokki.Application.Scheduling;

public static class OrganizationSchedulingPolicyWizard
{
    public static SchedulingPolicyWizardDraftResponse BuildDraft(SchedulingPolicyWizardRequest request)
    {
        var averageEmployees = Math.Max(1, request.AverageEmployees);
        var shiftsPerDay = Math.Max(1, request.ShiftsPerDay);

        var rules = SchedulingRuleCatalog.GetDefaultRuleDtos().ToList();

        Enable(rules, "require_submitted_preferences");
        Enable(rules, "unavailable_is_hard_block");

        if (shiftsPerDay >= 2)
            SetEnabledNumber(rules, "min_rest_minutes_between_shifts", 480);

        if (averageEmployees >= 3)
            SetEnabledNumber(rules, "max_shifts_per_employee_per_week", Math.Min(20, shiftsPerDay * 7));

        if (averageEmployees >= 5)
            SetEnabledNumber(rules, "max_shifts_per_employee_per_day", Math.Min(2, shiftsPerDay));

        return new SchedulingPolicyWizardDraftResponse(
            SchedulingRuleCatalog.SchemaVersion,
            rules,
            BuildSummary(averageEmployees, shiftsPerDay));
    }

    private static void Enable(List<SchedulingRuleDto> rules, string key)
    {
        var index = rules.FindIndex(rule => rule.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return;
        rules[index] = rules[index] with { Enabled = true };
    }

    private static void SetEnabledNumber(List<SchedulingRuleDto> rules, string key, int value)
    {
        var index = rules.FindIndex(rule => rule.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return;

        var rule = rules[index];
        rules[index] = rule with
        {
            Enabled = true,
            Value = System.Text.Json.JsonSerializer.SerializeToElement(value)
        };
    }

    private static string BuildSummary(int averageEmployees, int shiftsPerDay) =>
        $"Gợi ý cho ~{averageEmployees} nhân viên và ~{shiftsPerDay} ca/ngày. Hãy bật/tắt và chỉnh số trước khi lưu.";
}
