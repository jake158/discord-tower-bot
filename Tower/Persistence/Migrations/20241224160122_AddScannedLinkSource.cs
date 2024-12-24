using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tower.Persistence.Migrations
{
    public partial class AddScannedLinkSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScanSource",
                table: "ScannedLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScanSource",
                table: "ScannedLinks");
        }
    }
}
