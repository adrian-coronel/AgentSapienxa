using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentSapienxa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEmbeddingDimTo768 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // nomic-embed-text (Ollama) produce 768 dims, no 1536 (OpenAI).
            // Hay que limpiar chunks existentes antes de cambiar el tipo
            // porque pgvector no puede castear entre dimensiones distintas.
            migrationBuilder.Sql("DELETE FROM document_chunks;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_document_chunks_embedding;");
            migrationBuilder.Sql("ALTER TABLE document_chunks ALTER COLUMN embedding TYPE vector(768);");
            migrationBuilder.Sql(
                "CREATE INDEX ix_document_chunks_embedding " +
                "ON document_chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM document_chunks;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_document_chunks_embedding;");
            migrationBuilder.Sql("ALTER TABLE document_chunks ALTER COLUMN embedding TYPE vector(1536);");
            migrationBuilder.Sql(
                "CREATE INDEX ix_document_chunks_embedding " +
                "ON document_chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);");
        }
    }
}
