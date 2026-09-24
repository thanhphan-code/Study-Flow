using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DocumentStorageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "documents",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "application/octet-stream");

            migrationBuilder.Sql("""
                UPDATE documents SET "MimeType" = CASE "FileType"
                    WHEN 'Pdf' THEN 'application/pdf'
                    WHEN 'Docx' THEN 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
                    WHEN 'Pptx' THEN 'application/vnd.openxmlformats-officedocument.presentationml.presentation'
                    WHEN 'Txt' THEN 'text/plain'
                    ELSE 'application/octet-stream'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "documents");
        }
    }
}
