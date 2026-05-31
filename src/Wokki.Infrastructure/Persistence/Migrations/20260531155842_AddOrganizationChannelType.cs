using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationChannelType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_channels_OrganizationId",
                table: "channels");

            migrationBuilder.CreateIndex(
                name: "IX_channels_OrganizationId_Type",
                table: "channels",
                columns: new[] { "OrganizationId", "Type" },
                unique: true,
                filter: "\"Type\" = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_channels_OrganizationId_Type",
                table: "channels");

            migrationBuilder.CreateIndex(
                name: "IX_channels_OrganizationId",
                table: "channels",
                column: "OrganizationId");
        }
    }
}
