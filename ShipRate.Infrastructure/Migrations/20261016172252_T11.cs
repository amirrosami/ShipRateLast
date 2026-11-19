using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "base",
                table: "ChargeTypes",
                keyColumn: "Id",
                keyValue: 140,
                column: "ChargeTypeLocalName",
                value: "سپرده چک تضمین");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "base",
                table: "ChargeTypes",
                keyColumn: "Id",
                keyValue: 140,
                column: "ChargeTypeLocalName",
                value: "سپرده غیرنقدی");
        }
    }
}
