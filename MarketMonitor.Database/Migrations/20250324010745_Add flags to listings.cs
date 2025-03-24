using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addflagstolistings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "Listings");

            migrationBuilder.AddColumn<int>(
                name: "Flags",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flags",
                table: "Listings");

            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "Listings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
