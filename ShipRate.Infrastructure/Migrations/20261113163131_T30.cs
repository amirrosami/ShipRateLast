using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T30 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoparnCode",
                schema: "base",
                table: "TradeTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoparnCode",
                schema: "base",
                table: "LoadTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 99,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 20,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 21,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 22,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 23,
                column: "CoparnCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 24,
                column: "CoparnCode",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoparnCode",
                schema: "base",
                table: "TradeTypes");

            migrationBuilder.DropColumn(
                name: "CoparnCode",
                schema: "base",
                table: "LoadTypes");
        }
    }
}
