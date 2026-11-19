using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BalanceAfterMovement",
                schema: "fin",
                table: "VaultMovements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceBeforeMovement",
                schema: "fin",
                table: "VaultMovements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceAfterMovement",
                schema: "fin",
                table: "VaultMovements");

            migrationBuilder.DropColumn(
                name: "BalanceBeforeMovement",
                schema: "fin",
                table: "VaultMovements");
        }
    }
}
