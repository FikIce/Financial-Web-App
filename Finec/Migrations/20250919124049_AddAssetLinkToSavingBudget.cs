using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finec.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetLinkToSavingBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "Budgets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_AssetId",
                table: "Budgets",
                column: "AssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Assets_AssetId",
                table: "Budgets",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_Assets_AssetId",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_AssetId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Budgets");
        }
    }
}
