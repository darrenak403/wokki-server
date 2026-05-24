using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LocationSchedulingPolicyRemovePublishRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE location_scheduling_policies
                SET "RulesJson" = COALESCE(
                    (
                        SELECT jsonb_agg(elem ORDER BY COALESCE((elem->>'sortOrder')::int, 0))
                        FROM jsonb_array_elements("RulesJson") elem
                        WHERE elem->>'key' NOT IN (
                            'require_manager_review_before_apply',
                            'auto_apply_suggestions',
                            'allow_partial_apply'
                        )
                    ),
                    '[]'::jsonb
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
