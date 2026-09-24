using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminControlCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActiveAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuspendedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_audit_logs_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admin_audit_logs_users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE users AS u
                SET "IsSuspended" = p."IsSuspended",
                    "SuspensionReason" = CASE WHEN p."IsSuspended" THEN 'Được chuyển từ trạng thái kiểm duyệt hiện có' ELSE NULL END,
                    "SuspendedAt" = CASE WHEN p."IsSuspended" THEN p."UpdatedAt" ELSE NULL END
                FROM user_profiles AS p
                WHERE p."UserId" = u."Id" AND p."IsSuspended" = TRUE;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_users_LastActiveAt",
                table: "users",
                column: "LastActiveAt");

            migrationBuilder.CreateIndex(
                name: "IX_users_Role_IsSuspended",
                table: "users",
                columns: new[] { "Role", "IsSuspended" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_ActorUserId_CreatedAt",
                table: "admin_audit_logs",
                columns: new[] { "ActorUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_TargetUserId_CreatedAt",
                table: "admin_audit_logs",
                columns: new[] { "TargetUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_users_LastActiveAt",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Role_IsSuspended",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastActiveAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                table: "users");
        }
    }
}
