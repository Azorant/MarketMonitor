using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Listingcompositekey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Listings_ListingId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ListingId",
                table: "Sales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Listings",
                table: "Listings");

            migrationBuilder.AddColumn<string>(
                name: "ListingRetainerName",
                table: "Sales",
                type: "varchar(64)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Listings",
                table: "Listings",
                columns: new[] { "Id", "RetainerName" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ListingId_ListingRetainerName",
                table: "Sales",
                columns: new[] { "ListingId", "ListingRetainerName" });

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Listings_ListingId_ListingRetainerName",
                table: "Sales",
                columns: new[] { "ListingId", "ListingRetainerName" },
                principalTable: "Listings",
                principalColumns: new[] { "Id", "RetainerName" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Listings_ListingId_ListingRetainerName",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ListingId_ListingRetainerName",
                table: "Sales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Listings",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ListingRetainerName",
                table: "Sales");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Listings",
                table: "Listings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ListingId",
                table: "Sales",
                column: "ListingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Listings_ListingId",
                table: "Sales",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
