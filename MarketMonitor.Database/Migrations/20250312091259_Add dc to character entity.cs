using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Adddctocharacterentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatacenterName",
                table: "Characters",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_DatacenterName",
                table: "Characters",
                column: "DatacenterName");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Datacenters_DatacenterName",
                table: "Characters",
                column: "DatacenterName",
                principalTable: "Datacenters",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Datacenters_DatacenterName",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_DatacenterName",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DatacenterName",
                table: "Characters");
        }
    }
}
