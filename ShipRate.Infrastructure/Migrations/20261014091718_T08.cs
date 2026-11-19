using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T08 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsThirdPartySettlement",
                schema: "fin",
                table: "PaymentDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ROE",
                schema: "fin",
                table: "PaymentDocuments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedPaymentDocumentId",
                schema: "fin",
                table: "PaymentDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ThirdPartyId",
                schema: "fin",
                table: "PaymentDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentDocumentItems",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNo = table.Column<int>(type: "int", nullable: false),
                    ScanDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MachineSerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SiteName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Denomination = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FitResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDocumentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentDocumentItems_PaymentDocuments_PaymentDocumentId",
                        column: x => x.PaymentDocumentId,
                        principalSchema: "fin",
                        principalTable: "PaymentDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentDocumentItemImages",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentDocumentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleteUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDocumentItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentDocumentItemImages_PaymentDocumentItems_PaymentDocumentItemId",
                        column: x => x.PaymentDocumentItemId,
                        principalSchema: "fin",
                        principalTable: "PaymentDocumentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDocuments_RelatedPaymentDocumentId",
                schema: "fin",
                table: "PaymentDocuments",
                column: "RelatedPaymentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDocuments_ThirdPartyId",
                schema: "fin",
                table: "PaymentDocuments",
                column: "ThirdPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDocumentItemImages_PaymentDocumentItemId",
                schema: "fin",
                table: "PaymentDocumentItemImages",
                column: "PaymentDocumentItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDocumentItems_PaymentDocumentId",
                schema: "fin",
                table: "PaymentDocumentItems",
                column: "PaymentDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentDocuments_PaymentDocuments_RelatedPaymentDocumentId",
                schema: "fin",
                table: "PaymentDocuments",
                column: "RelatedPaymentDocumentId",
                principalSchema: "fin",
                principalTable: "PaymentDocuments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentDocuments_Persons_ThirdPartyId",
                schema: "fin",
                table: "PaymentDocuments",
                column: "ThirdPartyId",
                principalSchema: "base",
                principalTable: "Persons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentDocuments_PaymentDocuments_RelatedPaymentDocumentId",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentDocuments_Persons_ThirdPartyId",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DropTable(
                name: "PaymentDocumentItemImages",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "PaymentDocumentItems",
                schema: "fin");

            migrationBuilder.DropIndex(
                name: "IX_PaymentDocuments_RelatedPaymentDocumentId",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentDocuments_ThirdPartyId",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DropColumn(
                name: "IsThirdPartySettlement",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DropColumn(
                name: "ROE",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DropColumn(
                name: "RelatedPaymentDocumentId",
                schema: "fin",
                table: "PaymentDocuments");

            migrationBuilder.DropColumn(
                name: "ThirdPartyId",
                schema: "fin",
                table: "PaymentDocuments");
        }
    }
}
