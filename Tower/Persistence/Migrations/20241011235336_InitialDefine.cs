using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tower.Persistence.Migrations
{
    public partial class InitialDefine : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Blacklisted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    UserID = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPremium = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_Guilds_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStats",
                columns: table => new
                {
                    UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    TotalScans = table.Column<int>(type: "int", nullable: false),
                    ScansToday = table.Column<int>(type: "int", nullable: false),
                    LastScanDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStats", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    IsScanEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AlertChannel = table.Column<decimal>(type: "decimal(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_GuildSettings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildStats",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    TotalScans = table.Column<int>(type: "int", nullable: false),
                    MalwareFoundCount = table.Column<int>(type: "int", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildStats", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_GuildStats_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOffenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    GuildID = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    OffenseDetails = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OffenseDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOffenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOffenses_Guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "Guilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOffenses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_UserID",
                table: "Guilds",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOffenses_GuildID",
                table: "UserOffenses",
                column: "GuildID");

            migrationBuilder.CreateIndex(
                name: "IX_UserOffenses_UserId",
                table: "UserOffenses",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildSettings");

            migrationBuilder.DropTable(
                name: "GuildStats");

            migrationBuilder.DropTable(
                name: "UserOffenses");

            migrationBuilder.DropTable(
                name: "UserStats");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
