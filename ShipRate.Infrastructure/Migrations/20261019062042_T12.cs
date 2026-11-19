using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SettlementItemPreviousPayments",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementItemPaymentAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SettledAmountInItemCurrency = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExchangeRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExchangeRateBaseId = table.Column<int>(type: "int", nullable: true),
                    ExchangeRateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExchangeRateValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAmountInBaseCurrency = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_SettlementItemPreviousPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementItemPreviousPayments_ExchangeRateBases_ExchangeRateBaseId",
                        column: x => x.ExchangeRateBaseId,
                        principalSchema: "base",
                        principalTable: "ExchangeRateBases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SettlementItemPreviousPayments_ExchangeRates_ExchangeRateId",
                        column: x => x.ExchangeRateId,
                        principalSchema: "fin",
                        principalTable: "ExchangeRates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SettlementItemPreviousPayments_SettlementItemPaymentAllocations_SettlementItemPaymentAllocationId",
                        column: x => x.SettlementItemPaymentAllocationId,
                        principalSchema: "fin",
                        principalTable: "SettlementItemPaymentAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementItemPreviousPayments_SettlementItems_SettlementItemId",
                        column: x => x.SettlementItemId,
                        principalSchema: "fin",
                        principalTable: "SettlementItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementItemPreviousPayments_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalSchema: "fin",
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItemPreviousPayments_ExchangeRateBaseId",
                schema: "fin",
                table: "SettlementItemPreviousPayments",
                column: "ExchangeRateBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItemPreviousPayments_ExchangeRateId",
                schema: "fin",
                table: "SettlementItemPreviousPayments",
                column: "ExchangeRateId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItemPreviousPayments_SettlementId",
                schema: "fin",
                table: "SettlementItemPreviousPayments",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItemPreviousPayments_SettlementItemId",
                schema: "fin",
                table: "SettlementItemPreviousPayments",
                column: "SettlementItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItemPreviousPayments_SettlementItemPaymentAllocationId",
                schema: "fin",
                table: "SettlementItemPreviousPayments",
                column: "SettlementItemPaymentAllocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SettlementItemPreviousPayments",
                schema: "fin");
        }
    }
}
