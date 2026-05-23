using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SchedulePreferencesAndStaffingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxStaffPerSlot",
                table: "shift_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "JobPositionId",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "department_scheduling_policies",
                columns: table => new
                {
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxShiftsPerEmployeePerWeek = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_scheduling_policies", x => x.DepartmentId);
                    table.ForeignKey(
                        name: "FK_department_scheduling_policies_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetHeadcount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_positions_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_preference_submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_preference_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_preference_submissions_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_preference_submissions_schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_preference_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferenceType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_preference_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_preference_lines_schedule_preference_submissions_S~",
                        column: x => x.SubmissionId,
                        principalTable: "schedule_preference_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_preference_lines_shift_definitions_ShiftDefinition~",
                        column: x => x.ShiftDefinitionId,
                        principalTable: "shift_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employees_JobPositionId",
                table: "employees",
                column: "JobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_job_positions_DepartmentId_Code",
                table: "job_positions",
                columns: new[] { "DepartmentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_preference_lines_ShiftDefinitionId",
                table: "schedule_preference_lines",
                column: "ShiftDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_preference_lines_SubmissionId_ShiftDefinitionId_Da~",
                table: "schedule_preference_lines",
                columns: new[] { "SubmissionId", "ShiftDefinitionId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_preference_submissions_EmployeeId",
                table: "schedule_preference_submissions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_preference_submissions_ScheduleId_EmployeeId",
                table: "schedule_preference_submissions",
                columns: new[] { "ScheduleId", "EmployeeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_job_positions_JobPositionId",
                table: "employees",
                column: "JobPositionId",
                principalTable: "job_positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employees_job_positions_JobPositionId",
                table: "employees");

            migrationBuilder.DropTable(
                name: "department_scheduling_policies");

            migrationBuilder.DropTable(
                name: "job_positions");

            migrationBuilder.DropTable(
                name: "schedule_preference_lines");

            migrationBuilder.DropTable(
                name: "schedule_preference_submissions");

            migrationBuilder.DropIndex(
                name: "IX_employees_JobPositionId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "MaxStaffPerSlot",
                table: "shift_definitions");

            migrationBuilder.DropColumn(
                name: "JobPositionId",
                table: "employees");
        }
    }
}
