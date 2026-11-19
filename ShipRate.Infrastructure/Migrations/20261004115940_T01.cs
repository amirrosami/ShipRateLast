using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MainContactEmail",
                schema: "base",
                table: "Persons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainContactName",
                schema: "base",
                table: "Persons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainContactPhone",
                schema: "base",
                table: "Persons",
                type: "nvarchar(35)",
                maxLength: 35,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainContactEmail",
                schema: "base",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "MainContactName",
                schema: "base",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "MainContactPhone",
                schema: "base",
                table: "Persons");
        }
    }
}
