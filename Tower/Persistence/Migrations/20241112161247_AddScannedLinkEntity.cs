using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tower.Persistence.Migrations
{
    public partial class AddScannedLinkEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffenseDetails",
                table: "UserOffenses");

            migrationBuilder.AddColumn<int>(
                name: "ScannedLinkId",
                table: "UserOffenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ScannedLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LinkHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsMalware = table.Column<bool>(type: "bit", nullable: false),
                    IsSuspicious = table.Column<bool>(type: "bit", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MD5hash = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ExpireTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScannedLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOffenses_ScannedLinkId",
                table: "UserOffenses",
                column: "ScannedLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_ScannedLinks_LinkHash",
                table: "ScannedLinks",
                column: "LinkHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOffenses_ScannedLinks_ScannedLinkId",
                table: "UserOffenses",
                column: "ScannedLinkId",
                principalTable: "ScannedLinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOffenses_ScannedLinks_ScannedLinkId",
                table: "UserOffenses");

            migrationBuilder.DropTable(
                name: "ScannedLinks");

            migrationBuilder.DropIndex(
                name: "IX_UserOffenses_ScannedLinkId",
                table: "UserOffenses");

            migrationBuilder.DropColumn(
                name: "ScannedLinkId",
                table: "UserOffenses");

            migrationBuilder.AddColumn<string>(
                name: "OffenseDetails",
                table: "UserOffenses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
