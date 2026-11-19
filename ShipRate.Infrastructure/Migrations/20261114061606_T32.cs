using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T32 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "sec",
                table: "Permissions",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "DisplayName", "IsActive", "Name" },
                values: new object[,]
                {
                    { 302001, "Finance", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "View Payment Requests", true, "Finance.PaymentRequests.View" },
                    { 302002, "Finance", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Edit Payment Requests", true, "Finance.PaymentRequests.Edit" },
                    { 302005, "Finance", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Send Payment Requests", true, "Finance.PaymentRequests.SendRequest" },
                    { 302010, "Finance", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Confirm Payment Requests", true, "Finance.PaymentRequests.ConfirmRequest" },
                    { 302015, "Finance", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Pay Payment Requests", true, "Finance.PaymentRequests.PayRequest" },
                    { 302099, "Finance", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Reject Payment Requests", true, "Finance.PaymentRequests.RejectRequest" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 302001);

            migrationBuilder.DeleteData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 302002);

            migrationBuilder.DeleteData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 302005);

            migrationBuilder.DeleteData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 302010);

            migrationBuilder.DeleteData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 302015);

            migrationBuilder.DeleteData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 302099);
        }
    }
}
