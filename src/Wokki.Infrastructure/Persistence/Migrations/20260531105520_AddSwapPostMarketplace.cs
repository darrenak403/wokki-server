using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSwapPostMarketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "swap_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcceptedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptorAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_swap_posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_swap_posts_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_swap_posts_employees_AcceptedByEmployeeId",
                        column: x => x.AcceptedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_swap_posts_employees_AuthorEmployeeId",
                        column: x => x.AuthorEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_swap_posts_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_swap_posts_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_swap_posts_schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_swap_posts_shift_assignments_AcceptorAssignmentId",
                        column: x => x.AcceptorAssignmentId,
                        principalTable: "shift_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_swap_posts_shift_assignments_AuthorAssignmentId",
                        column: x => x.AuthorAssignmentId,
                        principalTable: "shift_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_swap_posts_AcceptedByEmployeeId",
                table: "swap_posts",
                column: "AcceptedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_swap_posts_AcceptorAssignmentId",
                table: "swap_posts",
                column: "AcceptorAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_swap_posts_author_assignment_pending_unique",
                table: "swap_posts",
                column: "AuthorAssignmentId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_swap_posts_AuthorEmployeeId",
                table: "swap_posts",
                column: "AuthorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_swap_posts_DepartmentId",
                table: "swap_posts",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_swap_posts_LocationId",
                table: "swap_posts",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_swap_posts_OrganizationId",
                table: "swap_posts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_swap_posts_ScheduleId_DepartmentId_Status",
                table: "swap_posts",
                columns: new[] { "ScheduleId", "DepartmentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "swap_posts");
        }
    }
}
