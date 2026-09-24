using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistSmartLearningSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableReverseRecall",
                table: "flashcards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "smart_learning_cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudySessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlashcardId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    AttemptType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NextEligibleAttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_smart_learning_cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_smart_learning_cards_flashcards_FlashcardId",
                        column: x => x.FlashcardId,
                        principalTable: "flashcards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_smart_learning_cards_study_sessions_StudySessionId",
                        column: x => x.StudySessionId,
                        principalTable: "study_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_smart_learning_cards_FlashcardId",
                table: "smart_learning_cards",
                column: "FlashcardId");

            migrationBuilder.CreateIndex(
                name: "IX_smart_learning_cards_StudySessionId_FlashcardId",
                table: "smart_learning_cards",
                columns: new[] { "StudySessionId", "FlashcardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_smart_learning_cards_StudySessionId_Outcome_NextEligibleAtt~",
                table: "smart_learning_cards",
                columns: new[] { "StudySessionId", "Outcome", "NextEligibleAttemptNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "smart_learning_cards");

            migrationBuilder.DropColumn(
                name: "EnableReverseRecall",
                table: "flashcards");
        }
    }
}
