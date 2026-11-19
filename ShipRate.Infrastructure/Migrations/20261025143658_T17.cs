using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "fin",
                table: "InvoiceSubjects",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "InvoiceSubjectLocalName", "InvoiceSubjectName" },
                values: new object[] { "صورتحساب سپرده نقدی", "Cash Warranty Invoice" });

            migrationBuilder.InsertData(
                schema: "fin",
                table: "InvoiceSubjects",
                columns: new[] { "Id", "InvoiceSubjectLocalName", "InvoiceSubjectName", "IsActive", "IsDeleted" },
                values: new object[] { 11, "صورتحساب سپرده غیرنقدی(چک)", "Cheque Warranty Invoice", true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "fin",
                table: "InvoiceSubjects",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.UpdateData(
                schema: "fin",
                table: "InvoiceSubjects",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "InvoiceSubjectLocalName", "InvoiceSubjectName" },
                values: new object[] { "صورتحساب سپرده ها", "Warranty Invoice" });
        }
    }
}
