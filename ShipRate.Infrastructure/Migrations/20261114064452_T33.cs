using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T33 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "sec",
                table: "Permissions",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "DisplayName", "IsActive", "Name" },
                values: new object[] { 302012, "Finance", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Approve(Final Confirm) Payment Requests", true, "Finance.PaymentRequests.ApproveRequest" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 302012);
        }
    }
}
