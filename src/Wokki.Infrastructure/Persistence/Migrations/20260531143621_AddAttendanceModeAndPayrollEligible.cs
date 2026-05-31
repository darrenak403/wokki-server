using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceModeAndPayrollEligible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "payroll_lines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidMarkedBy",
                table: "payroll_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegularMinutes",
                table: "payroll_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceModePolicy",
                table: "departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EligibleMarkedAt",
                table: "attendance_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EligibleMarkedBy",
                table: "attendance_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "attendance_records",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PayrollEligible",
                table: "attendance_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "payroll_lines");

            migrationBuilder.DropColumn(
                name: "PaidMarkedBy",
                table: "payroll_lines");

            migrationBuilder.DropColumn(
                name: "RegularMinutes",
                table: "payroll_lines");

            migrationBuilder.DropColumn(
                name: "AttendanceModePolicy",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "EligibleMarkedAt",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "EligibleMarkedBy",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "PayrollEligible",
                table: "attendance_records");
        }
    }
}
