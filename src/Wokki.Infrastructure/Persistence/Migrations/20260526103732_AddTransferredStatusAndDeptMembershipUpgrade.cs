using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferredStatusAndDeptMembershipUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe pattern: add nullable, backfill, then constrain non-null.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "employee_department_memberships",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                table: "employee_department_memberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftAt",
                table: "employee_department_memberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE employee_department_memberships
                SET "Status" = 1, "JoinedAt" = "CreatedAt"
                WHERE "Status" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "employee_department_memberships",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "employee_department_memberships",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "employee_department_memberships");

            migrationBuilder.DropColumn(
                name: "LeftAt",
                table: "employee_department_memberships");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "employee_department_memberships");
        }
    }
}
