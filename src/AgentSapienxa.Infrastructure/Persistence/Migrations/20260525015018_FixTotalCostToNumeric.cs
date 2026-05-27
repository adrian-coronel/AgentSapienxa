using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentSapienxa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTotalCostToNumeric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE leads_enrollments ALTER COLUMN total_cost TYPE numeric(18,2) USING total_cost::numeric;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE leads_enrollments ALTER COLUMN total_cost TYPE text USING total_cost::text;");
        }
    }
}
