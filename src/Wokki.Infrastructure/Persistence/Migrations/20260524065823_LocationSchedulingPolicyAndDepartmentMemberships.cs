using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LocationSchedulingPolicyAndDepartmentMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "schedule_insight_contexts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "schedule_insight_contexts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "schedule_insight_contexts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateOnly>(
                name: "WeekStartDate",
                table: "schedule_insight_contexts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "employee_department_memberships",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_department_memberships", x => new { x.EmployeeId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_employee_department_memberships_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_department_memberships_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "location_scheduling_policies",
                columns: table => new
                {
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxHoursPerDay = table.Column<int>(type: "integer", nullable: false),
                    MaxHoursPerWeek = table.Column<int>(type: "integer", nullable: false),
                    MaxShiftsPerDay = table.Column<int>(type: "integer", nullable: false),
                    MaxShiftsPerWeek = table.Column<int>(type: "integer", nullable: false),
                    MinShiftsPerWeek = table.Column<int>(type: "integer", nullable: false),
                    AllowOvertime = table.Column<bool>(type: "boolean", nullable: false),
                    MinRestMinutesBetweenShifts = table.Column<int>(type: "integer", nullable: false),
                    WeeklyRestDaysRequired = table.Column<int>(type: "integer", nullable: false),
                    MaxConsecutiveWorkDays = table.Column<int>(type: "integer", nullable: false),
                    BreakRequiredAfterMinutes = table.Column<int>(type: "integer", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    RequireFullCoverage = table.Column<bool>(type: "boolean", nullable: false),
                    AllowUnderstaffedSuggestions = table.Column<bool>(type: "boolean", nullable: false),
                    CoverageByRoleRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultMinStaffPerShift = table.Column<int>(type: "integer", nullable: false),
                    DefaultMaxStaffPerShift = table.Column<int>(type: "integer", nullable: true),
                    RequireDepartmentMembership = table.Column<bool>(type: "boolean", nullable: false),
                    RequireRoleMatch = table.Column<bool>(type: "boolean", nullable: false),
                    AllowTerminatedEmployees = table.Column<bool>(type: "boolean", nullable: false),
                    RequireActiveEmployee = table.Column<bool>(type: "boolean", nullable: false),
                    RequireSubmittedPreferences = table.Column<bool>(type: "boolean", nullable: false),
                    UnavailableIsHardBlock = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredWeight = table.Column<int>(type: "integer", nullable: false),
                    AvailableWeight = table.Column<int>(type: "integer", nullable: false),
                    MissingPreferencePenalty = table.Column<int>(type: "integer", nullable: false),
                    BalanceShiftCount = table.Column<bool>(type: "boolean", nullable: false),
                    FairnessWeight = table.Column<int>(type: "integer", nullable: false),
                    BalanceWeekendShifts = table.Column<bool>(type: "boolean", nullable: false),
                    AvoidSameEmployeeAlwaysSameShift = table.Column<bool>(type: "boolean", nullable: false),
                    AvoidOvertime = table.Column<bool>(type: "boolean", nullable: false),
                    OvertimePenaltyWeight = table.Column<int>(type: "integer", nullable: false),
                    PreferLowerCostWhenEqual = table.Column<bool>(type: "boolean", nullable: false),
                    RequireManagerReviewBeforeApply = table.Column<bool>(type: "boolean", nullable: false),
                    AutoApplySuggestions = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPartialApply = table.Column<bool>(type: "boolean", nullable: false),
                    CustomRulesJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_location_scheduling_policies", x => x.LocationId);
                    table.ForeignKey(
                        name: "FK_location_scheduling_policies_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO employee_department_memberships ("EmployeeId", "DepartmentId", "IsPrimary", "CreatedAt")
                SELECT "Id", "DepartmentId", TRUE, "CreatedAt"
                FROM employees
                ON CONFLICT ("EmployeeId", "DepartmentId") DO NOTHING;
                """);

            migrationBuilder.Sql("""
                UPDATE schedule_insight_contexts AS context
                SET
                    "DepartmentId" = schedule."DepartmentId",
                    "WeekStartDate" = schedule."WeekStartDate",
                    "LocationId" = department."LocationId",
                    "ExpiresAt" = (schedule."WeekStartDate"::timestamp AT TIME ZONE 'UTC') + INTERVAL '14 days'
                FROM schedules AS schedule
                INNER JOIN departments AS department ON department."Id" = schedule."DepartmentId"
                WHERE context."ScheduleId" = schedule."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_insight_contexts_DepartmentId_WeekStartDate",
                table: "schedule_insight_contexts",
                columns: new[] { "DepartmentId", "WeekStartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_insight_contexts_ExpiresAt",
                table: "schedule_insight_contexts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_memberships_DepartmentId",
                table: "employee_department_memberships",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_department_memberships");

            migrationBuilder.DropTable(
                name: "location_scheduling_policies");

            migrationBuilder.DropIndex(
                name: "IX_schedule_insight_contexts_DepartmentId_WeekStartDate",
                table: "schedule_insight_contexts");

            migrationBuilder.DropIndex(
                name: "IX_schedule_insight_contexts_ExpiresAt",
                table: "schedule_insight_contexts");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "schedule_insight_contexts");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "schedule_insight_contexts");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "schedule_insight_contexts");

            migrationBuilder.DropColumn(
                name: "WeekStartDate",
                table: "schedule_insight_contexts");
        }
    }
}
