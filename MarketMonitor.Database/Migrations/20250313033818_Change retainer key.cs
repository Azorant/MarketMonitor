using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    /// <inheritdoc />
    public partial class Changeretainerkey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Retainers_RetainerId",
                table: "Listings");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Retainers_Id",
                table: "Retainers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Retainers",
                table: "Retainers");

            migrationBuilder.DropIndex(
                name: "IX_Listings_RetainerId",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "RetainerId",
                table: "Listings",
                newName: "RetainerName");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Retainers",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<ulong>(
                name: "RetainerOwnerId",
                table: "Listings",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Retainers",
                table: "Retainers",
                columns: new[] { "Name", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_RetainerName_RetainerOwnerId",
                table: "Listings",
                columns: new[] { "RetainerName", "RetainerOwnerId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Retainers_RetainerName_RetainerOwnerId",
                table: "Listings",
                columns: new[] { "RetainerName", "RetainerOwnerId" },
                principalTable: "Retainers",
                principalColumns: new[] { "Name", "OwnerId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Retainers_RetainerName_RetainerOwnerId",
                table: "Listings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Retainers",
                table: "Retainers");

            migrationBuilder.DropIndex(
                name: "IX_Listings_RetainerName_RetainerOwnerId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "RetainerOwnerId",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "RetainerName",
                table: "Listings",
                newName: "RetainerId");

            migrationBuilder.UpdateData(
                table: "Retainers",
                keyColumn: "Id",
                keyValue: null,
                column: "Id",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Retainers",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Retainers_Id",
                table: "Retainers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Retainers",
                table: "Retainers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_RetainerId",
                table: "Listings",
                column: "RetainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Retainers_RetainerId",
                table: "Listings",
                column: "RetainerId",
                principalTable: "Retainers",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
