using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformActivityEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_activity_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_activity_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_platform_activity_events_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_platform_activity_events_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_platform_activity_events_EventType_OccurredAt",
                table: "platform_activity_events",
                columns: new[] { "EventType", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_platform_activity_events_OrganizationId_OccurredAt",
                table: "platform_activity_events",
                columns: new[] { "OrganizationId", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_platform_activity_events_UserId",
                table: "platform_activity_events",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_activity_events");
        }
    }
}
