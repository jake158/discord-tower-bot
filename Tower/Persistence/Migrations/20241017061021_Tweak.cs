using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tower.Persistence.Migrations
{
    public partial class Tweak : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_Users_UserID",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOffenses_Guilds_GuildID",
                table: "UserOffenses");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "GuildID",
                table: "UserOffenses",
                newName: "GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_UserOffenses_GuildID",
                table: "UserOffenses",
                newName: "IX_UserOffenses_GuildId");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Guilds",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Guilds_UserID",
                table: "Guilds",
                newName: "IX_Guilds_UserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastScanDate",
                table: "UserStats",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastScanDate",
                table: "GuildStats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScansToday",
                table: "GuildStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_Users_UserId",
                table: "Guilds",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffenses_Guilds_GuildId",
                table: "UserOffenses",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_Users_UserId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOffenses_Guilds_GuildId",
                table: "UserOffenses");

            migrationBuilder.DropColumn(
                name: "LastScanDate",
                table: "GuildStats");

            migrationBuilder.DropColumn(
                name: "ScansToday",
                table: "GuildStats");

            migrationBuilder.RenameColumn(
                name: "GuildId",
                table: "UserOffenses",
                newName: "GuildID");

            migrationBuilder.RenameIndex(
                name: "IX_UserOffenses_GuildId",
                table: "UserOffenses",
                newName: "IX_UserOffenses_GuildID");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Guilds",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Guilds_UserId",
                table: "Guilds",
                newName: "IX_Guilds_UserID");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastScanDate",
                table: "UserStats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_Users_UserID",
                table: "Guilds",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffenses_Guilds_GuildID",
                table: "UserOffenses",
                column: "GuildID",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
