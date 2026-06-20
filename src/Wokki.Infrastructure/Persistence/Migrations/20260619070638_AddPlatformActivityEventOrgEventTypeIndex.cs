using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformActivityEventOrgEventTypeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_platform_activity_events_OrganizationId_EventType_OccurredAt",
                table: "platform_activity_events",
                columns: new[] { "OrganizationId", "EventType", "OccurredAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_platform_activity_events_OrganizationId_EventType_OccurredAt",
                table: "platform_activity_events");
        }
    }
}
