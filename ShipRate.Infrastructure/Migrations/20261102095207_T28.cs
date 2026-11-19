using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T28 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                schema: "cnt",
                table: "Detentions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detentions_BranchId",
                schema: "cnt",
                table: "Detentions",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Detentions_Branches_BranchId",
                schema: "cnt",
                table: "Detentions",
                column: "BranchId",
                principalSchema: "base",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detentions_Branches_BranchId",
                schema: "cnt",
                table: "Detentions");

            migrationBuilder.DropIndex(
                name: "IX_Detentions_BranchId",
                schema: "cnt",
                table: "Detentions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                schema: "cnt",
                table: "Detentions");
        }
    }
}
