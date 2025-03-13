using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addhqtolistings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHq",
                table: "Listings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNotified",
                table: "Listings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHq",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "IsNotified",
                table: "Listings");
        }
    }
}
