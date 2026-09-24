using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DocumentPipelineV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveProcessingJobId",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "documents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingCompletedAt",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingRevision",
                table: "documents",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "pipeline-v2");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedAt",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "QueuedAt",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "document_chunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: true),
                    SlideNumber = table.Column<int>(type: "integer", nullable: true),
                    SectionTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartOffset = table.Column<int>(type: "integer", nullable: false),
                    EndOffset = table.Column<int>(type: "integer", nullable: false),
                    CharacterCount = table.Column<int>(type: "integer", nullable: false),
                    EstimatedTokenCount = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_chunks_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_processing_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_processing_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_processing_jobs_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE documents
                SET "ProcessingStatus" = CASE
                        WHEN "ExtractedText" IS NOT NULL AND length(trim("ExtractedText")) > 0 THEN 'Ready'
                        WHEN "ProcessingError" IS NOT NULL THEN 'Failed'
                        ELSE 'Uploaded'
                    END,
                    "ProcessingRevision" = 'legacy-v1',
                    "ProcessingCompletedAt" = CASE WHEN "ExtractedText" IS NOT NULL AND length(trim("ExtractedText")) > 0 THEN "UpdatedAt" ELSE NULL END,
                    "FailedAt" = CASE WHEN "ExtractedText" IS NULL AND "ProcessingError" IS NOT NULL THEN "UpdatedAt" ELSE NULL END,
                    "FailureCode" = CASE WHEN "ExtractedText" IS NULL AND "ProcessingError" IS NOT NULL THEN 'DOCUMENT_EXTRACTION_FAILED' ELSE NULL END;

                INSERT INTO document_chunks
                    ("Id", "DocumentId", "ChunkIndex", "Content", "PageNumber", "SlideNumber", "SectionTitle", "StartOffset", "EndOffset", "CharacterCount", "EstimatedTokenCount", "ContentHash", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), "Id", 0, "ExtractedText", NULL, NULL, NULL, 0, length("ExtractedText"), length("ExtractedText"), GREATEST(1, (length("ExtractedText") + 3) / 4), md5("ExtractedText"), "CreatedAt", "UpdatedAt"
                FROM documents
                WHERE "ProcessingStatus" = 'Ready' AND "ExtractedText" IS NOT NULL AND length(trim("ExtractedText")) > 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId_ChunkIndex",
                table: "document_chunks",
                columns: new[] { "DocumentId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId_ContentHash",
                table: "document_chunks",
                columns: new[] { "DocumentId", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_document_processing_jobs_DocumentId",
                table: "document_processing_jobs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_document_processing_jobs_Status_AvailableAt",
                table: "document_processing_jobs",
                columns: new[] { "Status", "AvailableAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE documents SET \"ProcessingStatus\" = 'Completed' WHERE \"ProcessingStatus\" = 'Ready';");
            migrationBuilder.DropTable(
                name: "document_chunks");

            migrationBuilder.DropTable(
                name: "document_processing_jobs");

            migrationBuilder.DropColumn(
                name: "ActiveProcessingJobId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ProcessingCompletedAt",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ProcessingRevision",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "QueuedAt",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "documents");
        }
    }
}
