using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImproveSmartLearningRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerRevealed",
                table: "learning_attempts");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientAttemptId",
                table: "learning_attempts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE learning_attempts SET \"ClientAttemptId\" = gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "learning_attempts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Forward");

            migrationBuilder.AddColumn<string>(
                name: "SubmittedAnswer",
                table: "learning_attempts",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SrsCommitted",
                table: "learning_attempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_learning_attempts_StudySessionId_ClientAttemptId",
                table: "learning_attempts",
                columns: new[] { "StudySessionId", "ClientAttemptId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_learning_attempts_StudySessionId_ClientAttemptId",
                table: "learning_attempts");

            migrationBuilder.DropColumn(
                name: "ClientAttemptId",
                table: "learning_attempts");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "learning_attempts");

            migrationBuilder.DropColumn(
                name: "SubmittedAnswer",
                table: "learning_attempts");

            migrationBuilder.DropColumn(
                name: "SrsCommitted",
                table: "learning_attempts");

            migrationBuilder.AddColumn<bool>(
                name: "AnswerRevealed",
                table: "learning_attempts",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
