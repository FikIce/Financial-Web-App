using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finec.Migrations
{
    /// <inheritdoc />
    public partial class V9_FinalCorrectedSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_AspNetUsers_UserId",
                table: "Budgets");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Budgets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_AccountId",
                table: "Budgets",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Accounts_AccountId",
                table: "Budgets",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_AspNetUsers_UserId",
                table: "Budgets",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_Accounts_AccountId",
                table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_AspNetUsers_UserId",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_AccountId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Budgets");

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_AspNetUsers_UserId",
                table: "Budgets",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
