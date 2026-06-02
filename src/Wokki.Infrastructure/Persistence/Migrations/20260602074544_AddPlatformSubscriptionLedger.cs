using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSubscriptionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_subscription_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PreviousDurationDays = table.Column<int>(type: "integer", nullable: false),
                    NewDurationDays = table.Column<int>(type: "integer", nullable: false),
                    PreviousExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_subscription_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_subscription_ledger_entries_organizations_Orga~",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_subscription_ledger_entries_users_ChangedByUse~",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_subscription_ledger_entries_Action",
                table: "organization_subscription_ledger_entries",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_organization_subscription_ledger_entries_ChangedByUserId",
                table: "organization_subscription_ledger_entries",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_subscription_ledger_entries_OrganizationId_Cha~",
                table: "organization_subscription_ledger_entries",
                columns: new[] { "OrganizationId", "ChangedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_subscription_ledger_entries");
        }
    }
}
