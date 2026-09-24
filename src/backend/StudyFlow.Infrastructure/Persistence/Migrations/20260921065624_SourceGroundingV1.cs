using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SourceGroundingV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroundingContextJson",
                table: "ai_jobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetrievalRevision",
                table: "ai_jobs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "source_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentChunkId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: true),
                    SlideNumber = table.Column<int>(type: "integer", nullable: true),
                    SectionTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartOffset = table.Column<int>(type: "integer", nullable: false),
                    EndOffset = table.Column<int>(type: "integer", nullable: false),
                    ChunkContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RetrievalRevision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GroundingRevision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DocumentNameSnapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SourceSnippetSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_source_references_document_chunks_DocumentChunkId",
                        column: x => x.DocumentChunkId,
                        principalTable: "document_chunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_source_references_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_source_references_ContentType_ContentId",
                table: "source_references",
                columns: new[] { "ContentType", "ContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_source_references_DocumentChunkId",
                table: "source_references",
                column: "DocumentChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_source_references_DocumentId",
                table: "source_references",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "source_references");

            migrationBuilder.DropColumn(
                name: "GroundingContextJson",
                table: "ai_jobs");

            migrationBuilder.DropColumn(
                name: "RetrievalRevision",
                table: "ai_jobs");
        }
    }
}
