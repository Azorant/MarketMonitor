using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Cacheitemicon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "Items",
                newName: "IconPath");

            migrationBuilder.AddColumn<byte[]>(
                name: "IconData",
                table: "Items",
                type: "longblob",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconData",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "IconPath",
                table: "Items",
                newName: "Icon");
        }
    }
}
