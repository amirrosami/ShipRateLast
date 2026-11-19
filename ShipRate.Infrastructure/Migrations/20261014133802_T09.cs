using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T09 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ROE",
                schema: "fin",
                table: "PaymentDocuments",
                newName: "ExchangeRate");

            migrationBuilder.AddColumn<byte>(
                name: "ThirdPartyRole",
                schema: "fin",
                table: "PaymentDocuments",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThirdPartyRole",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.RenameColumn(
                name: "ExchangeRate",
                schema: "fin",
                table: "PaymentDocuments",
                newName: "ROE");
        }
    }
}
