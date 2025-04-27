using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addundercutbool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UndercutNotification",
                table: "Characters",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UndercutNotification",
                table: "Characters");
        }
    }
}
