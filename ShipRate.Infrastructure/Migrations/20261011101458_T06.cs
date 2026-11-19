using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SystemId",
                schema: "fin",
                table: "TreasuryDocuments",
                type: "bigint",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR dbo.Seq_TreasuryDocumentSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryDocuments_SystemId",
                schema: "fin",
                table: "TreasuryDocuments",
                column: "SystemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreasuryDocuments_SystemId",
                schema: "fin",
                table: "TreasuryDocuments");

            migrationBuilder.DropColumn(
                name: "SystemId",
                schema: "fin",
                table: "TreasuryDocuments");
        }
    }
}
