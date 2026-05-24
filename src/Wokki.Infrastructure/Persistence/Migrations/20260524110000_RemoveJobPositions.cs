using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJobPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employees_job_positions_JobPositionId",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_employees_JobPositionId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "JobPositionId",
                table: "employees");

            migrationBuilder.DropTable(
                name: "job_positions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetHeadcount = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_job_positions_DepartmentId_Code",
                table: "job_positions",
                columns: new[] { "DepartmentId", "Code" },
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobPositionId",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_JobPositionId",
                table: "employees",
                column: "JobPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_employees_job_positions_JobPositionId",
                table: "employees",
                column: "JobPositionId",
                principalTable: "job_positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
