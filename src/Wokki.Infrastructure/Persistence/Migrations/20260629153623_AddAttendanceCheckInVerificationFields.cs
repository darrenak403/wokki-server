using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceCheckInVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "locations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "locations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkIpOrCidr",
                table: "locations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaceEmbedding",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaceEnrollmentPhotoPublicId",
                table: "employees",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaceEnrollmentPhotoUrl",
                table: "employees",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockInIpAddress",
                table: "attendance_records",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ClockInLatitude",
                table: "attendance_records",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ClockInLongitude",
                table: "attendance_records",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockInPhotoPublicId",
                table: "attendance_records",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockInPhotoUrl",
                table: "attendance_records",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FaceMismatch",
                table: "attendance_records",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GpsOutOfRange",
                table: "attendance_records",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IpMismatch",
                table: "attendance_records",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "NetworkIpOrCidr",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "FaceEmbedding",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "FaceEnrollmentPhotoPublicId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "FaceEnrollmentPhotoUrl",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ClockInIpAddress",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "ClockInLatitude",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "ClockInLongitude",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "ClockInPhotoPublicId",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "ClockInPhotoUrl",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "FaceMismatch",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "GpsOutOfRange",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "IpMismatch",
                table: "attendance_records");
        }
    }
}
