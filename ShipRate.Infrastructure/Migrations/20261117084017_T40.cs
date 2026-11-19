using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T40 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_PaymentDocuments_PaymentDocumentId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_PaymentDocumentId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "PaymentDocumentId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentRequestId",
                schema: "fin",
                table: "PaymentDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.InsertData(
                schema: "base",
                table: "EntityTypes",
                columns: new[] { "Id", "EntityTypeName", "IsActive", "IsDeleted" },
                values: new object[] { 49, "LegDispatch", true, false });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDocuments_PaymentRequestId",
                schema: "fin",
                table: "PaymentDocuments",
                column: "PaymentRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentDocuments_PaymentRequests_PaymentRequestId",
                schema: "fin",
                table: "PaymentDocuments",
                column: "PaymentRequestId",
                principalSchema: "fin",
                principalTable: "PaymentRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentDocuments_PaymentRequests_PaymentRequestId",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentDocuments_PaymentRequestId",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DeleteData(
                schema: "base",
                table: "EntityTypes",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DropColumn(
                name: "PaymentRequestId",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentDocumentId",
                schema: "fin",
                table: "PaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_PaymentDocumentId",
                schema: "fin",
                table: "PaymentRequests",
                column: "PaymentDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRequests_PaymentDocuments_PaymentDocumentId",
                schema: "fin",
                table: "PaymentRequests",
                column: "PaymentDocumentId",
                principalSchema: "fin",
                principalTable: "PaymentDocuments",
                principalColumn: "Id");
        }
    }
}
