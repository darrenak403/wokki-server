using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LocationSchedulingPolicyRuleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomRulesJson",
                table: "location_scheduling_policies",
                newName: "RulesJson");

            migrationBuilder.Sql("""
                UPDATE location_scheduling_policies
                SET "RulesJson" = jsonb_build_array(
                    jsonb_build_object('key','max_hours_per_day','category','workHourLimits','content','Nhân viên không làm quá số giờ này trong một ngày.','inputLabel','Giờ tối đa / ngày','valueType','number','value',"MaxHoursPerDay",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',10),
                    jsonb_build_object('key','max_hours_per_week','category','workHourLimits','content','Nhân viên không làm quá số giờ này trong một tuần.','inputLabel','Giờ tối đa / tuần','valueType','number','value',"MaxHoursPerWeek",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',20),
                    jsonb_build_object('key','max_shifts_per_day','category','workHourLimits','content','Số ca tối đa một nhân viên được xếp trong một ngày.','inputLabel','Ca tối đa / ngày','valueType','number','value',"MaxShiftsPerDay",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',30),
                    jsonb_build_object('key','max_shifts_per_week','category','workHourLimits','content','Số ca tối đa một nhân viên được xếp trong một tuần.','inputLabel','Ca tối đa / tuần','valueType','number','value',"MaxShiftsPerWeek",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',40),
                    jsonb_build_object('key','min_shifts_per_week','category','workHourLimits','content','Mục tiêu số ca tối thiểu cho mỗi nhân viên trong tuần.','inputLabel','Ca tối thiểu / tuần','valueType','number','value',"MinShiftsPerWeek",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',50),
                    jsonb_build_object('key','allow_overtime','category','workHourLimits','content','Chi nhánh có cho phép gợi ý lịch tạo overtime hay không.','inputLabel','Cho phép tăng ca','valueType','boolean','value',"AllowOvertime",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',60),
                    jsonb_build_object('key','min_rest_minutes_between_shifts','category','restRules','content','Khoảng nghỉ tối thiểu giữa hai ca của cùng nhân viên.','inputLabel','Phút nghỉ giữa ca','valueType','number','value',"MinRestMinutesBetweenShifts",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',110),
                    jsonb_build_object('key','weekly_rest_days_required','category','restRules','content','Số ngày nghỉ cần có trong một tuần.','inputLabel','Ngày nghỉ / tuần','valueType','number','value',"WeeklyRestDaysRequired",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',120),
                    jsonb_build_object('key','max_consecutive_work_days','category','restRules','content','Số ngày làm liên tiếp tối đa trước khi cần nghỉ.','inputLabel','Ngày làm liên tiếp tối đa','valueType','number','value',"MaxConsecutiveWorkDays",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',130),
                    jsonb_build_object('key','break_required_after_minutes','category','restRules','content','Sau số phút làm việc này thì cần nghỉ giữa ca.','inputLabel','Nghỉ sau số phút làm','valueType','number','value',"BreakRequiredAfterMinutes",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',140),
                    jsonb_build_object('key','break_minutes','category','restRules','content','Thời lượng nghỉ giữa ca.','inputLabel','Số phút nghỉ','valueType','number','value',"BreakMinutes",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',150),
                    jsonb_build_object('key','require_full_coverage','category','coverageRules','content','Solver chỉ nên trả lịch đủ người theo capacity.','inputLabel','Yêu cầu đủ người','valueType','boolean','value',"RequireFullCoverage",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',210),
                    jsonb_build_object('key','allow_understaffed_suggestions','category','coverageRules','content','Cho phép solver trả gợi ý thiếu người khi không đủ dữ liệu.','inputLabel','Cho phép gợi ý thiếu người','valueType','boolean','value',"AllowUnderstaffedSuggestions",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',220),
                    jsonb_build_object('key','coverage_by_role_required','category','coverageRules','content','Coverage phải xét theo vai trò/position của ca.','inputLabel','Yêu cầu đủ theo vai trò','valueType','boolean','value',"CoverageByRoleRequired",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',230),
                    jsonb_build_object('key','default_min_staff_per_shift','category','coverageRules','content','Số người tối thiểu mặc định nếu ca không có rule riêng.','inputLabel','Số người tối thiểu / ca','valueType','number','value',"DefaultMinStaffPerShift",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',240),
                    jsonb_build_object('key','default_max_staff_per_shift','category','coverageRules','content','Số người tối đa mặc định nếu cần giới hạn thêm.','inputLabel','Số người tối đa / ca','valueType','number','value',"DefaultMaxStaffPerShift",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',250),
                    jsonb_build_object('key','require_department_membership','category','employeeEligibilityRules','content','Nhân viên phải thuộc phòng ban đang xếp lịch.','inputLabel','Bắt buộc thuộc phòng ban','valueType','boolean','value',"RequireDepartmentMembership",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',310),
                    jsonb_build_object('key','require_role_match','category','employeeEligibilityRules','content','Position nhân viên phải khớp RequiredRole của ca.','inputLabel','Bắt buộc khớp vai trò','valueType','boolean','value',"RequireRoleMatch",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',320),
                    jsonb_build_object('key','require_active_employee','category','employeeEligibilityRules','content','Chỉ xếp nhân viên đang active.','inputLabel','Chỉ nhân viên active','valueType','boolean','value',"RequireActiveEmployee",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',330),
                    jsonb_build_object('key','allow_terminated_employees','category','employeeEligibilityRules','content','Có cho phép nhân viên đã nghỉ xuất hiện trong gợi ý hay không.','inputLabel','Cho phép nhân viên đã nghỉ','valueType','boolean','value',"AllowTerminatedEmployees",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',340),
                    jsonb_build_object('key','require_submitted_preferences','category','preferenceRules','content','Yêu cầu có đăng ký ca đã gửi trước khi chạy auto-scheduling.','inputLabel','Bắt buộc có đăng ký ca','valueType','boolean','value',"RequireSubmittedPreferences",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',410),
                    jsonb_build_object('key','unavailable_is_hard_block','category','preferenceRules','content','Nếu nhân viên chọn unavailable thì solver không được xếp ca đó.','inputLabel','Unavailable là chặn cứng','valueType','boolean','value',"UnavailableIsHardBlock",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',420),
                    jsonb_build_object('key','preferred_weight','category','preferenceRules','content','Điểm cộng khi gợi ý khớp ca preferred.','inputLabel','Điểm ca preferred','valueType','number','value',"PreferredWeight",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',430),
                    jsonb_build_object('key','available_weight','category','preferenceRules','content','Điểm cộng khi dùng ca available.','inputLabel','Điểm ca available','valueType','number','value',"AvailableWeight",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',440),
                    jsonb_build_object('key','missing_preference_penalty','category','preferenceRules','content','Điểm phạt khi không có preference rõ ràng.','inputLabel','Phạt khi thiếu preference','valueType','number','value',"MissingPreferencePenalty",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',450),
                    jsonb_build_object('key','balance_shift_count','category','fairnessRules','content','Cân bằng số ca giữa nhân viên trong phòng ban.','inputLabel','Cân bằng số ca','valueType','boolean','value',"BalanceShiftCount",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',510),
                    jsonb_build_object('key','fairness_weight','category','fairnessRules','content','Trọng số ưu tiên công bằng.','inputLabel','Trọng số công bằng','valueType','number','value',"FairnessWeight",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',520),
                    jsonb_build_object('key','balance_weekend_shifts','category','fairnessRules','content','Cân bằng số ca cuối tuần.','inputLabel','Cân bằng ca cuối tuần','valueType','boolean','value',"BalanceWeekendShifts",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',530),
                    jsonb_build_object('key','avoid_same_employee_always_same_shift','category','fairnessRules','content','Tránh để một nhân viên luôn lặp cùng loại ca.','inputLabel','Tránh luôn cùng một ca','valueType','boolean','value',"AvoidSameEmployeeAlwaysSameShift",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',540),
                    jsonb_build_object('key','avoid_overtime','category','costRules','content','Giảm ưu tiên các phương án tạo overtime.','inputLabel','Tránh overtime','valueType','boolean','value',"AvoidOvertime",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',610),
                    jsonb_build_object('key','overtime_penalty_weight','category','costRules','content','Điểm phạt khi phương án tạo overtime.','inputLabel','Phạt overtime','valueType','number','value',"OvertimePenaltyWeight",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',620),
                    jsonb_build_object('key','prefer_lower_cost_when_equal','category','costRules','content','Khi điểm bằng nhau, ưu tiên nhân viên/ca có chi phí thấp hơn.','inputLabel','Ưu tiên chi phí thấp khi bằng điểm','valueType','boolean','value',"PreferLowerCostWhenEqual",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',630),
                    jsonb_build_object('key','require_manager_review_before_apply','category','publishRules','content','Manager/Admin phải review trước khi apply gợi ý.','inputLabel','Bắt buộc review trước apply','valueType','boolean','value',"RequireManagerReviewBeforeApply",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',710),
                    jsonb_build_object('key','auto_apply_suggestions','category','publishRules','content','Cho phép tự động apply gợi ý sau khi solver chạy.','inputLabel','Tự apply gợi ý','valueType','boolean','value',"AutoApplySuggestions",'enabled',true,'isDefault',true,'isRequired',true,'sortOrder',720),
                    jsonb_build_object('key','allow_partial_apply','category','publishRules','content','Cho phép manager chỉ apply một phần gợi ý.','inputLabel','Cho phép apply một phần','valueType','boolean','value',"AllowPartialApply",'enabled',true,'isDefault',true,'isRequired',false,'sortOrder',730)
                );
                """);

            migrationBuilder.DropColumn(
                name: "AllowOvertime",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "AllowPartialApply",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "AllowTerminatedEmployees",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "AllowUnderstaffedSuggestions",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "AutoApplySuggestions",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "AvailableWeight",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "AvoidOvertime",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "AvoidSameEmployeeAlwaysSameShift",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "BalanceShiftCount",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "BalanceWeekendShifts",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "BreakMinutes",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "BreakRequiredAfterMinutes",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "CoverageByRoleRequired",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "DefaultMaxStaffPerShift",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "DefaultMinStaffPerShift",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "FairnessWeight",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "MaxConsecutiveWorkDays",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "MaxHoursPerDay",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "MaxHoursPerWeek",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "MaxShiftsPerDay",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "MaxShiftsPerWeek",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "MinRestMinutesBetweenShifts",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "MinShiftsPerWeek",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "MissingPreferencePenalty",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "OvertimePenaltyWeight",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "PreferLowerCostWhenEqual",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "PreferredWeight",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "RequireActiveEmployee",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "RequireDepartmentMembership",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "RequireFullCoverage",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "RequireManagerReviewBeforeApply",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "RequireRoleMatch",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "RequireSubmittedPreferences",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "UnavailableIsHardBlock",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "WeeklyRestDaysRequired",
                table: "location_scheduling_policies");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RulesJson",
                table: "location_scheduling_policies",
                newName: "CustomRulesJson");

            migrationBuilder.AddColumn<bool>(
                name: "AllowOvertime",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPartialApply",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowTerminatedEmployees",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowUnderstaffedSuggestions",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoApplySuggestions",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AvailableWeight",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AvoidOvertime",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AvoidSameEmployeeAlwaysSameShift",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BalanceShiftCount",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BalanceWeekendShifts",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BreakMinutes",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BreakRequiredAfterMinutes",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CoverageByRoleRequired",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultMaxStaffPerShift",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultMinStaffPerShift",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FairnessWeight",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxConsecutiveWorkDays",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxHoursPerDay",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxHoursPerWeek",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxShiftsPerDay",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxShiftsPerWeek",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinRestMinutesBetweenShifts",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinShiftsPerWeek",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MissingPreferencePenalty",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OvertimePenaltyWeight",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PreferLowerCostWhenEqual",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PreferredWeight",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireActiveEmployee",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireDepartmentMembership",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireFullCoverage",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireManagerReviewBeforeApply",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireRoleMatch",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireSubmittedPreferences",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UnavailableIsHardBlock",
                table: "location_scheduling_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WeeklyRestDaysRequired",
                table: "location_scheduling_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
