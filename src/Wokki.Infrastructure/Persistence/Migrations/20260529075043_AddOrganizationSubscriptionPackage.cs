using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSubscriptionPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionActivatedAt",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionDurationDays",
                table: "organizations",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<bool>(
                name: "SubscriptionEnabled",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionExpiresAt",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionUpdatedAt",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionActivatedAt",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionDurationDays",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionEnabled",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionExpiresAt",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionUpdatedAt",
                table: "organizations");
        }
    }
}
