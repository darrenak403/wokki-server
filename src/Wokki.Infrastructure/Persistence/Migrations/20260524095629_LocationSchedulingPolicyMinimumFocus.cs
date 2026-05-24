using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wokki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LocationSchedulingPolicyMinimumFocus : Migration
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
                            'max_hours_per_day',
                            'max_hours_per_week',
                            'max_shifts_per_day',
                            'max_shifts_per_week',
                            'max_consecutive_work_days',
                            'default_max_staff_per_shift',
                            'preferred_weight',
                            'available_weight',
                            'missing_preference_penalty',
                            'role_balance_weight',
                            'fairness_weight',
                            'overtime_penalty_weight',
                            'prefer_lower_cost_when_equal'
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
