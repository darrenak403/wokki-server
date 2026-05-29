using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KeepDepartmentMembershipPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_employee_department_memberships",
                table: "employee_department_memberships");

            migrationBuilder.AddPrimaryKey(
                name: "PK_employee_department_memberships",
                table: "employee_department_memberships",
                columns: new[] { "EmployeeId", "DepartmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_memberships_EmployeeId_DepartmentId_Sta~",
                table: "employee_department_memberships",
                columns: new[] { "EmployeeId", "DepartmentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_employee_department_memberships",
                table: "employee_department_memberships");

            migrationBuilder.DropIndex(
                name: "IX_employee_department_memberships_EmployeeId_DepartmentId_Sta~",
                table: "employee_department_memberships");

            migrationBuilder.AddPrimaryKey(
                name: "PK_employee_department_memberships",
                table: "employee_department_memberships",
                columns: new[] { "EmployeeId", "DepartmentId" });
        }
    }
}
