using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCombinedStudySets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "study_sets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.CreateTable(
                name: "study_set_sources",
                columns: table => new
                {
                    CombinedStudySetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceStudySetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_set_sources", x => new { x.CombinedStudySetId, x.SourceStudySetId });
                    table.ForeignKey(
                        name: "FK_study_set_sources_study_sets_CombinedStudySetId",
                        column: x => x.CombinedStudySetId,
                        principalTable: "study_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_study_set_sources_study_sets_SourceStudySetId",
                        column: x => x.SourceStudySetId,
                        principalTable: "study_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_study_set_sources_CombinedStudySetId_OrderIndex",
                table: "study_set_sources",
                columns: new[] { "CombinedStudySetId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_set_sources_SourceStudySetId",
                table: "study_set_sources",
                column: "SourceStudySetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_set_sources");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "study_sets");
        }
    }
}
