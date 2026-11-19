using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChargeTypeNosaSettings",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ChargeTypeId = table.Column<int>(type: "int", nullable: false),
                    SellDebitAccountCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellDebitDetailCode1 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellDebitDetailCode2 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellDebitDetailCode3 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellDebitDetailCode4 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellDebitDetailCode5 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellDebitDetailCode6 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellCreditAccountCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellCreditDetailCode1 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellCreditDetailCode2 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellCreditDetailCode3 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellCreditDetailCode4 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellCreditDetailCode5 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellCreditDetailCode6 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyDebitAccountCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyDebitDetailCode1 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyDebitDetailCode2 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyDebitDetailCode3 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyDebitDetailCode4 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyDebitDetailCode5 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyDebitDetailCode6 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyCreditAccountCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyCreditDetailCode1 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyCreditDetailCode2 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyCreditDetailCode3 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyCreditDetailCode4 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyCreditDetailCode5 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BuyCreditDetailCode6 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeTypeNosaSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChargeTypeNosaSettings_ChargeTypes_ChargeTypeId",
                        column: x => x.ChargeTypeId,
                        principalSchema: "base",
                        principalTable: "ChargeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChargeTypeNosaSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "cmp",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "rpt",
                table: "Reports",
                keyColumn: "Id",
                keyValue: 401,
                column: "SortOrder",
                value: 3);

            migrationBuilder.UpdateData(
                schema: "rpt",
                table: "Reports",
                keyColumn: "Id",
                keyValue: 402,
                column: "SortOrder",
                value: 4);

            migrationBuilder.UpdateData(
                schema: "rpt",
                table: "Reports",
                keyColumn: "Id",
                keyValue: 403,
                column: "SortOrder",
                value: 5);

            migrationBuilder.InsertData(
                schema: "rpt",
                table: "Reports",
                columns: new[] { "Id", "ActionName", "CategoryId", "ControllerName", "Description", "FilterViewName", "IconClass", "IsActive", "IsDeleted", "ReportKey", "ReportLocalName", "ReportName", "ResultViewName", "SortOrder" },
                values: new object[] { 404, "Filter", 4, "Reports", null, "CustomersListFilter", "bi bi-receipt-cutoff", true, false, "InvoiceItem_List", "", "Invoice Item List", "CustomersListResult", 2 });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeTypeNosaSettings_ChargeTypeId",
                schema: "fin",
                table: "ChargeTypeNosaSettings",
                column: "ChargeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargeTypeNosaSettings_CompanyId",
                schema: "fin",
                table: "ChargeTypeNosaSettings",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargeTypeNosaSettings",
                schema: "fin");

            migrationBuilder.DeleteData(
                schema: "rpt",
                table: "Reports",
                keyColumn: "Id",
                keyValue: 404);

            migrationBuilder.UpdateData(
                schema: "rpt",
                table: "Reports",
                keyColumn: "Id",
                keyValue: 401,
                column: "SortOrder",
                value: 2);

            migrationBuilder.UpdateData(
                schema: "rpt",
                table: "Reports",
                keyColumn: "Id",
                keyValue: 402,
                column: "SortOrder",
                value: 3);

            migrationBuilder.UpdateData(
                schema: "rpt",
                table: "Reports",
                keyColumn: "Id",
                keyValue: 403,
                column: "SortOrder",
                value: 4);
        }
    }
}
