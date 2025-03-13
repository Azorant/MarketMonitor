using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addworldtolisting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorldId",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_WorldId",
                table: "Listings",
                column: "WorldId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Worlds_WorldId",
                table: "Listings",
                column: "WorldId",
                principalTable: "Worlds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Worlds_WorldId",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_WorldId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "WorldId",
                table: "Listings");
        }
    }
}
