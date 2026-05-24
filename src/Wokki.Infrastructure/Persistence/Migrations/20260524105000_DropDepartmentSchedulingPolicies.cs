using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropDepartmentSchedulingPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "department_scheduling_policies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
