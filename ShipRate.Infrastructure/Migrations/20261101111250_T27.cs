using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestSentDate",
                schema: "fin",
                table: "PaymentRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.InsertData(
                schema: "fin",
                table: "PaymentRequestStatus",
                columns: new[] { "Id", "IsActive", "IsDeleted", "PaymentRequestStatusLocalName", "PaymentRequestStatusName" },
                values: new object[,]
                {
                    { 10, true, false, "درافت", "Draft" },
                    { 15, true, false, "ارسال درخواست", "Request Sent" },
                    { 20, true, false, "تایید میانی شده", "Confirmed" },
                    { 30, true, false, "تایید نهایی شده", "Approved" },
                    { 40, true, false, "پرداخت شده", "Paid" },
                    { 99, true, false, "رد شده", "Rejected" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "PaymentRequestStatus",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DropColumn(
                name: "RequestSentDate",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.InsertData(
                schema: "fin",
                table: "PaymentRequestStatus",
                columns: new[] { "Id", "IsActive", "IsDeleted", "PaymentRequestStatusLocalName", "PaymentRequestStatusName" },
                values: new object[,]
                {
                    { 1, true, false, "درافت /درخواست اولیه", "Draft" },
                    { 2, true, false, "تایید میانی شده", "Confirmed" },
                    { 3, true, false, "تایید نهایی شده", "Approved" },
                    { 4, true, false, "پرداخت شده", "Paid" },
                    { 5, true, false, "رد شده", "Rejected" }
                });
        }
    }
}
