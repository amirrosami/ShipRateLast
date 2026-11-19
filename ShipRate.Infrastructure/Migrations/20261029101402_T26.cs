using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T26 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSentToTaxProvider",
                schema: "fin",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToTaxProviderAt",
                schema: "fin",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxProviderError",
                schema: "fin",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxProviderSendUserId",
                schema: "fin",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxProviderStatus",
                schema: "fin",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxProviderUid",
                schema: "fin",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TaxProviderSendUserId",
                schema: "fin",
                table: "Invoices",
                column: "TaxProviderSendUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_TaxProviderSendUserId",
                schema: "fin",
                table: "Invoices",
                column: "TaxProviderSendUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_TaxProviderSendUserId",
                schema: "fin",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TaxProviderSendUserId",
                schema: "fin",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsSentToTaxProvider",
                schema: "fin",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SentToTaxProviderAt",
                schema: "fin",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxProviderError",
                schema: "fin",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxProviderSendUserId",
                schema: "fin",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxProviderStatus",
                schema: "fin",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxProviderUid",
                schema: "fin",
                table: "Invoices");
        }
    }
}
