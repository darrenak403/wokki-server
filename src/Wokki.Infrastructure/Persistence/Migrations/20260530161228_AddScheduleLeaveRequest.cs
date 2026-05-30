using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleLeaveRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedule_leave_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_leave_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_leave_requests_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_leave_requests_EmployeeId",
                table: "schedule_leave_requests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_leave_requests_OrganizationId",
                table: "schedule_leave_requests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_leave_requests_pending_slot_unique",
                table: "schedule_leave_requests",
                columns: new[] { "ScheduleId", "EmployeeId", "ShiftDefinitionId", "Date" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_leave_requests_ScheduleId",
                table: "schedule_leave_requests",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_leave_requests_Status",
                table: "schedule_leave_requests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_leave_requests");
        }
    }
}
