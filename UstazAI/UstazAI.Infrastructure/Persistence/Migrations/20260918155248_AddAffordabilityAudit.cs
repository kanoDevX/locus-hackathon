using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UstazAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAffordabilityAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AffordabilityBudgetCeilingUsd",
                table: "Recommendations",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AffordabilityEffectiveCostUsd",
                table: "Recommendations",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AffordabilityTier",
                table: "Recommendations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                // "" is not a valid AffordabilityTier enum member — HasConversion<string>() calls
                // Enum.Parse on read, which throws on empty string. This is the exact same
                // migration-authoring mistake fixed twice already in this codebase's history
                // (FundingTrackPreference, RoadmapTasks.Resources) — the scaffolded default must
                // match a real member, not an empty placeholder. "Affordable" (enum ordinal 0) is
                // an honest-enough placeholder for any pre-existing row whose tier was never
                // actually computed, same spirit as this column not existing for them at all.
                defaultValue: "Affordable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffordabilityBudgetCeilingUsd",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "AffordabilityEffectiveCostUsd",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "AffordabilityTier",
                table: "Recommendations");
        }
    }
}
