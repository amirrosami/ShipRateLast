using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "rpt",
                table: "Reports",
                columns: new[] { "Id", "ActionName", "CategoryId", "ControllerName", "Description", "FilterViewName", "IconClass", "IsActive", "IsDeleted", "ReportKey", "ReportLocalName", "ReportName", "ResultViewName", "SortOrder" },
                values: new object[] { 203, "Filter", 2, "Reports", null, "RateRequestsListFilter", "bi bi-file-earmark-text", true, false, "BLs_List_Containers", "", "BLs List With Containers", "RateRequestsListResult", 4 });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConfirmations_CreateUserId",
                schema: "crm",
                table: "CustomerConfirmations",
                column: "CreateUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerConfirmations_Users_CreateUserId",
                schema: "crm",
                table: "CustomerConfirmations",
                column: "CreateUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerConfirmations_Users_CreateUserId",
                schema: "crm",
                table: "CustomerConfirmations");

            migrationBuilder.DropIndex(
                name: "IX_CustomerConfirmations_CreateUserId",
                schema: "crm",
                table: "CustomerConfirmations");

            migrationBuilder.DeleteData(
                schema: "rpt",
                table: "Reports",
                keyColumn: "Id",
                keyValue: 203);
        }
    }
}
