using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LiveBattleV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "battle_rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudySetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    JoinCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    LeaderboardMode = table.Column<string>(type: "text", nullable: false),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    DefaultTimeLimitSeconds = table.Column<int>(type: "integer", nullable: true),
                    CurrentQuestionIndex = table.Column<int>(type: "integer", nullable: false),
                    QuestionStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    QuestionEndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_battle_rooms_users_HostUserId",
                        column: x => x.HostUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "battle_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewStudySetId = table.Column<Guid>(type: "uuid", nullable: true),
                    BattleRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LastConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Combo = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_participants", x => x.Id);
                    table.UniqueConstraint("AK_battle_participants_BattleRoomId_UserId", x => new { x.BattleRoomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_battle_participants_battle_rooms_BattleRoomId",
                        column: x => x.BattleRoomId,
                        principalTable: "battle_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_battle_participants_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "battle_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlashcardId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionType = table.Column<string>(type: "text", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "text", nullable: false),
                    AcceptedAnswers = table.Column<string>(type: "text", nullable: true),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    OptionsJson = table.Column<string>(type: "text", nullable: false),
                    LeftItemsJson = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    TimeLimitSeconds = table.Column<int>(type: "integer", nullable: true),
                    SourceReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_battle_questions_battle_rooms_BattleRoomId",
                        column: x => x.BattleRoomId,
                        principalTable: "battle_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "battle_answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningFlashcardId = table.Column<Guid>(type: "uuid", nullable: true),
                    BattleRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAnswer = table.Column<string>(type: "text", nullable: false),
                    Evaluation = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "integer", nullable: false),
                    ScoreEarned = table.Column<int>(type: "integer", nullable: false),
                    TimedOut = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_battle_answers_battle_participants_BattleRoomId_UserId",
                        columns: x => new { x.BattleRoomId, x.UserId },
                        principalTable: "battle_participants",
                        principalColumns: new[] { "BattleRoomId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_battle_answers_battle_questions_BattleQuestionId",
                        column: x => x.BattleQuestionId,
                        principalTable: "battle_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_battle_answers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_battle_answers_BattleQuestionId_UserId",
                table: "battle_answers",
                columns: new[] { "BattleQuestionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_battle_answers_BattleRoomId_UserId",
                table: "battle_answers",
                columns: new[] { "BattleRoomId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_battle_answers_UserId",
                table: "battle_answers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_battle_participants_BattleRoomId_UserId",
                table: "battle_participants",
                columns: new[] { "BattleRoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_battle_participants_UserId",
                table: "battle_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_battle_questions_BattleRoomId_OrderIndex",
                table: "battle_questions",
                columns: new[] { "BattleRoomId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_battle_rooms_HostUserId",
                table: "battle_rooms",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_battle_rooms_JoinCode",
                table: "battle_rooms",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_battle_rooms_Status_ExpiresAt",
                table: "battle_rooms",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_answers");

            migrationBuilder.DropTable(
                name: "battle_participants");

            migrationBuilder.DropTable(
                name: "battle_questions");

            migrationBuilder.DropTable(
                name: "battle_rooms");
        }
    }
}
