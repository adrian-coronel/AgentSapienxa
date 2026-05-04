using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentSapienxa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    system_prompt = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric", nullable: false),
                    max_tokens = table.Column<int>(type: "integer", nullable: true),
                    memory_window = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conversation_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    tool_calls = table.Column<string>(type: "text", nullable: true),
                    tokens_in = table.Column<int>(type: "integer", nullable: true),
                    tokens_out = table.Column<int>(type: "integer", nullable: true),
                    model = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instructors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    profile_picture = table.Column<string>(type: "text", nullable: true),
                    expertise = table.Column<string>(type: "text", nullable: true),
                    instructor_summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instructors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leads_enrollments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead = table.Column<Guid>(type: "uuid", nullable: false),
                    course = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    observation = table.Column<string>(type: "text", nullable: true),
                    total_cost = table.Column<string>(type: "text", nullable: true),
                    payment_method = table.Column<Guid>(type: "uuid", nullable: true),
                    voucher = table.Column<string>(type: "text", nullable: true),
                    sale_agent = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads_enrollments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image = table.Column<string>(type: "text", nullable: true),
                    limit_amount = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_validations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    voucher_detail = table.Column<string>(type: "text", nullable: true),
                    voucher_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    observation = table.Column<string>(type: "text", nullable: true),
                    requested_by = table.Column<string>(type: "text", nullable: true),
                    resolved_by = table.Column<string>(type: "text", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_validations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    lead_classification_summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_agents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    short_description = table.Column<string>(type: "text", nullable: true),
                    features = table.Column<string>(type: "text", nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    syllabus = table.Column<string>(type: "text", nullable: true),
                    projects = table.Column<string>(type: "text", nullable: true),
                    link = table.Column<string>(type: "text", nullable: true),
                    instructors = table.Column<Guid>(type: "uuid", nullable: true),
                    cost = table.Column<string>(type: "text", nullable: false),
                    places = table.Column<string>(type: "text", nullable: true),
                    available_places = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.id);
                    table.ForeignKey(
                        name: "FK_courses_instructors_instructors",
                        column: x => x.instructors,
                        principalTable: "instructors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    contact_method = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    sales_agent = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.id);
                    table.ForeignKey(
                        name: "FK_leads_sales_agents_sales_agent",
                        column: x => x.sales_agent,
                        principalTable: "sales_agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_config_agent_key",
                table: "agent_config",
                column: "agent_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversation_history_session_id_created_at",
                table: "conversation_history",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_courses_instructors",
                table: "courses",
                column: "instructors");

            migrationBuilder.CreateIndex(
                name: "IX_leads_phone_number",
                table: "leads",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leads_sales_agent",
                table: "leads",
                column: "sales_agent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_config");

            migrationBuilder.DropTable(
                name: "conversation_history");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropTable(
                name: "leads_enrollments");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "payment_validations");

            migrationBuilder.DropTable(
                name: "instructors");

            migrationBuilder.DropTable(
                name: "sales_agents");
        }
    }
}
