using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T07 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VAT",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VATName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VATLocalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VATPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VAT", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "fin",
                table: "VAT",
                columns: new[] { "Id", "IsActive", "IsDeleted", "VATLocalName", "VATName", "VATPercent" },
                values: new object[,]
                {
                    { 1, true, false, "صفر درصد", "0 Pecent", 0m },
                    { 2, true, false, "ده درصد", "10 Pecent", 0m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VAT",
                schema: "fin");
        }
    }
}
