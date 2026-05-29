using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_locations_Name",
                table: "locations");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "swap_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "shift_definitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "shift_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "schedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "schedule_preference_submissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "schedule_preference_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "schedule_insight_contexts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "payroll_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "pay_periods",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "overtime_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "locations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "location_scheduling_policies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "location_memberships",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "location_managers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "employees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "employee_department_memberships",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "employee_availabilities",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "departments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "channels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "channel_members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "attendance_records",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_OrganizationId",
                table: "users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_swap_requests_OrganizationId",
                table: "swap_requests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_definitions_OrganizationId",
                table: "shift_definitions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_assignments_OrganizationId",
                table: "shift_assignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_OrganizationId_CreatedAt",
                table: "schedules",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_preference_submissions_OrganizationId",
                table: "schedule_preference_submissions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_preference_lines_OrganizationId",
                table: "schedule_preference_lines",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_insight_contexts_OrganizationId",
                table: "schedule_insight_contexts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_lines_OrganizationId",
                table: "payroll_lines",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_pay_periods_OrganizationId",
                table: "pay_periods",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_requests_OrganizationId",
                table: "overtime_requests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_OrganizationId",
                table: "messages",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_locations_OrganizationId_Name",
                table: "locations",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_location_scheduling_policies_OrganizationId",
                table: "location_scheduling_policies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_location_memberships_OrganizationId",
                table: "location_memberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_location_managers_OrganizationId",
                table: "location_managers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_OrganizationId_CreatedAt",
                table: "employees",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_memberships_OrganizationId",
                table: "employee_department_memberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_availabilities_OrganizationId",
                table: "employee_availabilities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_OrganizationId",
                table: "departments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_channels_OrganizationId",
                table: "channels",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_channel_members_OrganizationId",
                table: "channel_members",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_OrganizationId",
                table: "audit_logs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_OrganizationId",
                table: "attendance_records",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Name",
                table: "organizations",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_records_organizations_OrganizationId",
                table: "attendance_records",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_organizations_OrganizationId",
                table: "audit_logs",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_channel_members_organizations_OrganizationId",
                table: "channel_members",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_channels_organizations_OrganizationId",
                table: "channels",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_departments_organizations_OrganizationId",
                table: "departments",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_availabilities_organizations_OrganizationId",
                table: "employee_availabilities",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_department_memberships_organizations_OrganizationId",
                table: "employee_department_memberships",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_organizations_OrganizationId",
                table: "employees",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_location_managers_organizations_OrganizationId",
                table: "location_managers",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_location_memberships_organizations_OrganizationId",
                table: "location_memberships",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_location_scheduling_policies_organizations_OrganizationId",
                table: "location_scheduling_policies",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_locations_organizations_OrganizationId",
                table: "locations",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_messages_organizations_OrganizationId",
                table: "messages",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_overtime_requests_organizations_OrganizationId",
                table: "overtime_requests",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pay_periods_organizations_OrganizationId",
                table: "pay_periods",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payroll_lines_organizations_OrganizationId",
                table: "payroll_lines",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_insight_contexts_organizations_OrganizationId",
                table: "schedule_insight_contexts",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_preference_lines_organizations_OrganizationId",
                table: "schedule_preference_lines",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_preference_submissions_organizations_OrganizationId",
                table: "schedule_preference_submissions",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_schedules_organizations_OrganizationId",
                table: "schedules",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shift_assignments_organizations_OrganizationId",
                table: "shift_assignments",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shift_definitions_organizations_OrganizationId",
                table: "shift_definitions",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_swap_requests_organizations_OrganizationId",
                table: "swap_requests",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_organizations_OrganizationId",
                table: "users",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendance_records_organizations_OrganizationId",
                table: "attendance_records");

            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_organizations_OrganizationId",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_channel_members_organizations_OrganizationId",
                table: "channel_members");

            migrationBuilder.DropForeignKey(
                name: "FK_channels_organizations_OrganizationId",
                table: "channels");

            migrationBuilder.DropForeignKey(
                name: "FK_departments_organizations_OrganizationId",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_availabilities_organizations_OrganizationId",
                table: "employee_availabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_department_memberships_organizations_OrganizationId",
                table: "employee_department_memberships");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_organizations_OrganizationId",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_location_managers_organizations_OrganizationId",
                table: "location_managers");

            migrationBuilder.DropForeignKey(
                name: "FK_location_memberships_organizations_OrganizationId",
                table: "location_memberships");

            migrationBuilder.DropForeignKey(
                name: "FK_location_scheduling_policies_organizations_OrganizationId",
                table: "location_scheduling_policies");

            migrationBuilder.DropForeignKey(
                name: "FK_locations_organizations_OrganizationId",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "FK_messages_organizations_OrganizationId",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "FK_overtime_requests_organizations_OrganizationId",
                table: "overtime_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_pay_periods_organizations_OrganizationId",
                table: "pay_periods");

            migrationBuilder.DropForeignKey(
                name: "FK_payroll_lines_organizations_OrganizationId",
                table: "payroll_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_schedule_insight_contexts_organizations_OrganizationId",
                table: "schedule_insight_contexts");

            migrationBuilder.DropForeignKey(
                name: "FK_schedule_preference_lines_organizations_OrganizationId",
                table: "schedule_preference_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_schedule_preference_submissions_organizations_OrganizationId",
                table: "schedule_preference_submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_schedules_organizations_OrganizationId",
                table: "schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_shift_assignments_organizations_OrganizationId",
                table: "shift_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_shift_definitions_organizations_OrganizationId",
                table: "shift_definitions");

            migrationBuilder.DropForeignKey(
                name: "FK_swap_requests_organizations_OrganizationId",
                table: "swap_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_users_organizations_OrganizationId",
                table: "users");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_users_OrganizationId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_swap_requests_OrganizationId",
                table: "swap_requests");

            migrationBuilder.DropIndex(
                name: "IX_shift_definitions_OrganizationId",
                table: "shift_definitions");

            migrationBuilder.DropIndex(
                name: "IX_shift_assignments_OrganizationId",
                table: "shift_assignments");

            migrationBuilder.DropIndex(
                name: "IX_schedules_OrganizationId_CreatedAt",
                table: "schedules");

            migrationBuilder.DropIndex(
                name: "IX_schedule_preference_submissions_OrganizationId",
                table: "schedule_preference_submissions");

            migrationBuilder.DropIndex(
                name: "IX_schedule_preference_lines_OrganizationId",
                table: "schedule_preference_lines");

            migrationBuilder.DropIndex(
                name: "IX_schedule_insight_contexts_OrganizationId",
                table: "schedule_insight_contexts");

            migrationBuilder.DropIndex(
                name: "IX_payroll_lines_OrganizationId",
                table: "payroll_lines");

            migrationBuilder.DropIndex(
                name: "IX_pay_periods_OrganizationId",
                table: "pay_periods");

            migrationBuilder.DropIndex(
                name: "IX_overtime_requests_OrganizationId",
                table: "overtime_requests");

            migrationBuilder.DropIndex(
                name: "IX_messages_OrganizationId",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_locations_OrganizationId_Name",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "IX_location_scheduling_policies_OrganizationId",
                table: "location_scheduling_policies");

            migrationBuilder.DropIndex(
                name: "IX_location_memberships_OrganizationId",
                table: "location_memberships");

            migrationBuilder.DropIndex(
                name: "IX_location_managers_OrganizationId",
                table: "location_managers");

            migrationBuilder.DropIndex(
                name: "IX_employees_OrganizationId_CreatedAt",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employee_department_memberships_OrganizationId",
                table: "employee_department_memberships");

            migrationBuilder.DropIndex(
                name: "IX_employee_availabilities_OrganizationId",
                table: "employee_availabilities");

            migrationBuilder.DropIndex(
                name: "IX_departments_OrganizationId",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_channels_OrganizationId",
                table: "channels");

            migrationBuilder.DropIndex(
                name: "IX_channel_members_OrganizationId",
                table: "channel_members");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_OrganizationId",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_attendance_records_OrganizationId",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "swap_requests");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "shift_definitions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "shift_assignments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "schedules");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "schedule_preference_submissions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "schedule_preference_lines");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "schedule_insight_contexts");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "payroll_lines");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "pay_periods");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "overtime_requests");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "location_scheduling_policies");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "location_memberships");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "location_managers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "employee_department_memberships");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "employee_availabilities");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "channel_members");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "attendance_records");

            migrationBuilder.CreateIndex(
                name: "IX_locations_Name",
                table: "locations",
                column: "Name",
                unique: true);
        }
    }
}
