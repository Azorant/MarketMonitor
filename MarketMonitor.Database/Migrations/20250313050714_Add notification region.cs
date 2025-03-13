using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addnotificationregion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationRegionId",
                table: "Characters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_NotificationRegionId",
                table: "Characters",
                column: "NotificationRegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Worlds_NotificationRegionId",
                table: "Characters",
                column: "NotificationRegionId",
                principalTable: "Worlds",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Worlds_NotificationRegionId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_NotificationRegionId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "NotificationRegionId",
                table: "Characters");
        }
    }
}
