using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_swap_requests_RequesterAssignmentId",
                table: "swap_requests");

            migrationBuilder.DropIndex(
                name: "IX_swap_requests_TargetAssignmentId",
                table: "swap_requests");

            migrationBuilder.CreateIndex(
                name: "IX_swap_requests_requester_assignment_peer_accepted",
                table: "swap_requests",
                column: "RequesterAssignmentId",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_swap_requests_target_assignment_peer_accepted",
                table: "swap_requests",
                column: "TargetAssignmentId",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_employee_open",
                table: "attendance_records",
                column: "EmployeeId",
                unique: true,
                filter: "\"ClockOut\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_swap_requests_requester_assignment_peer_accepted",
                table: "swap_requests");

            migrationBuilder.DropIndex(
                name: "IX_swap_requests_target_assignment_peer_accepted",
                table: "swap_requests");

            migrationBuilder.DropIndex(
                name: "IX_attendance_records_employee_open",
                table: "attendance_records");

            migrationBuilder.CreateIndex(
                name: "IX_swap_requests_RequesterAssignmentId",
                table: "swap_requests",
                column: "RequesterAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_swap_requests_TargetAssignmentId",
                table: "swap_requests",
                column: "TargetAssignmentId");
        }
    }
}
