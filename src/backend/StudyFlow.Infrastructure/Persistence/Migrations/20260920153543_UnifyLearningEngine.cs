using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnifyLearningEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_learning_attempts_StudySessionId_ClientAttemptId",
                table: "learning_attempts");

            migrationBuilder.AlterColumn<Guid>(
                name: "StudySessionId",
                table: "learning_attempts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CommittedRating",
                table: "learning_attempts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "learning_attempts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Learn");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptAt",
                table: "flashcard_progress",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MasteryScore",
                table: "flashcard_progress",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "flashcard_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "WeaknessScore",
                table: "flashcard_progress",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE flashcard_progress
                SET "LastAttemptAt" = "LastReviewedAt",
                    "Revision" = "CorrectCount" + "WrongCount",
                    "MasteryScore" = LEAST(100, CASE "Status"
                        WHEN 'Mastered' THEN 55
                        WHEN 'Reviewing' THEN 35
                        WHEN 'Learning' THEN 15
                        ELSE 0 END
                        + CASE WHEN "CorrectCount" + "WrongCount" = 0 THEN 0
                          ELSE "CorrectCount"::numeric / ("CorrectCount" + "WrongCount") * 30 END
                        + LEAST(15, "IntervalDays"::numeric / 2)),
                    "WeaknessScore" = LEAST(100,
                        CASE WHEN "CorrectCount" + "WrongCount" = 0 THEN 0
                          ELSE "WrongCount"::numeric / ("CorrectCount" + "WrongCount") * 55 END
                        + CASE WHEN "Status" = 'Learning' THEN 15 ELSE 0 END
                        + GREATEST(0, 2.5 - "EaseFactor") * 20)
                WHERE "LastReviewedAt" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_learning_attempts_UserId_ClientAttemptId",
                table: "learning_attempts",
                columns: new[] { "UserId", "ClientAttemptId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_learning_attempts_UserId_ClientAttemptId",
                table: "learning_attempts");

            migrationBuilder.DropColumn(
                name: "CommittedRating",
                table: "learning_attempts");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "learning_attempts");

            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "MasteryScore",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "flashcard_progress");

            migrationBuilder.DropColumn(
                name: "WeaknessScore",
                table: "flashcard_progress");

            migrationBuilder.AlterColumn<Guid>(
                name: "StudySessionId",
                table: "learning_attempts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_attempts_StudySessionId_ClientAttemptId",
                table: "learning_attempts",
                columns: new[] { "StudySessionId", "ClientAttemptId" },
                unique: true);
        }
    }
}
