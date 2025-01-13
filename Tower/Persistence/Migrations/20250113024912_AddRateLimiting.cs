using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tower.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRateLimiting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RateLimited",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RateLimited",
                table: "Users");
        }
    }
}
