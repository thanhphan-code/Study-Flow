using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MemoryEngineV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Revision",
                table: "flashcard_progress",
                newName: "StateRevision");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "UTC");

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveCorrect",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveWrong",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HintedCorrectCount",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCorrectAt",
                table: "flashcard_progress",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastScheduledAt",
                table: "flashcard_progress",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastWrongAt",
                table: "flashcard_progress",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecentWrongCount",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SchedulerVersion",
                table: "flashcard_progress",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.AddColumn<int>(
                name: "SuccessfulRecallCount",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SuccessfulRecognitionCount",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimedAttemptCount",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "TotalResponseTimeMs",
                table: "flashcard_progress",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "UnhintedCorrectCount",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE flashcard_progress
                SET "SchedulerVersion" = 'simple-srs/1',
                    "LastScheduledAt" = "LastReviewedAt",
                    "SuccessfulRecallCount" = "CorrectCount",
                    "UnhintedCorrectCount" = "CorrectCount",
                    "RecentWrongCount" = LEAST(3, "WrongCount"),
                    "ConsecutiveWrong" = CASE WHEN "Status" = 'Learning' THEN LEAST(3, "WrongCount") ELSE 0 END;
                """);

            migrationBuilder.CreateTable(
                name: "learning_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningAttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    MasteryBefore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MasteryAfter = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    WeaknessBefore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    WeaknessAfter = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IntervalBefore = table.Column<int>(type: "integer", nullable: false),
                    IntervalAfter = table.Column<int>(type: "integer", nullable: false),
                    NextReviewBefore = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextReviewAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActionBefore = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionAfter = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StatePolicyVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SchedulerVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DecisionTrace = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_decisions_learning_attempts_LearningAttemptId",
                        column: x => x.LearningAttemptId,
                        principalTable: "learning_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learning_decisions_LearningAttemptId",
                table: "learning_decisions",
                column: "LearningAttemptId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_decisions");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ConsecutiveCorrect",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "ConsecutiveWrong",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "HintedCorrectCount",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "LastCorrectAt",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "LastScheduledAt",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "LastWrongAt",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "RecentWrongCount",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "SchedulerVersion",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "UnhintedCorrectCount",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "SuccessfulRecallCount",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "SuccessfulRecognitionCount",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "TimedAttemptCount",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "TotalResponseTimeMs",
                table: "flashcard_progress");

            migrationBuilder.RenameColumn(
                name: "StateRevision",
                table: "flashcard_progress",
                newName: "Revision");
        }
    }
}
