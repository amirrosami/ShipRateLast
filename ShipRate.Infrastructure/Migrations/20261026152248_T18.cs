using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceRequestId",
                schema: "fin",
                table: "PaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStatusDate",
                schema: "fin",
                table: "Cheques",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceRequestPaymentDocuments",
                schema: "req",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_ServiceRequestPaymentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRequestPaymentDocuments_PaymentDocuments_PaymentDocumentId",
                        column: x => x.PaymentDocumentId,
                        principalSchema: "fin",
                        principalTable: "PaymentDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRequestPaymentDocuments_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "req",
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "base",
                table: "ServiceRequestTypes",
                columns: new[] { "Id", "IsActive", "IsDeleted", "LocalName", "Name" },
                values: new object[] { 10, true, false, "درخواست عودت بستانکاری /چک", "Return Cash Or Chaeque Request" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ServiceRequestId",
                schema: "fin",
                table: "PaymentRequests",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BLs_CreateUserId",
                schema: "ops",
                table: "BLs",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BLs_EditUserId",
                schema: "ops",
                table: "BLs",
                column: "EditUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BLs_MasterBLReleaseTypeId",
                schema: "ops",
                table: "BLs",
                column: "MasterBLReleaseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BLs_MasterPaymentTypeId",
                schema: "ops",
                table: "BLs",
                column: "MasterPaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestPaymentDocuments_PaymentDocumentId",
                schema: "req",
                table: "ServiceRequestPaymentDocuments",
                column: "PaymentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequestPaymentDocuments_ServiceRequestId",
                schema: "req",
                table: "ServiceRequestPaymentDocuments",
                column: "ServiceRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_BLs_BLReleaseTypes_MasterBLReleaseTypeId",
                schema: "ops",
                table: "BLs",
                column: "MasterBLReleaseTypeId",
                principalSchema: "base",
                principalTable: "BLReleaseTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BLs_PaymentTypes_MasterPaymentTypeId",
                schema: "ops",
                table: "BLs",
                column: "MasterPaymentTypeId",
                principalSchema: "base",
                principalTable: "PaymentTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BLs_Users_CreateUserId",
                schema: "ops",
                table: "BLs",
                column: "CreateUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BLs_Users_EditUserId",
                schema: "ops",
                table: "BLs",
                column: "EditUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRequests_ServiceRequests_ServiceRequestId",
                schema: "fin",
                table: "PaymentRequests",
                column: "ServiceRequestId",
                principalSchema: "req",
                principalTable: "ServiceRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BLs_BLReleaseTypes_MasterBLReleaseTypeId",
                schema: "ops",
                table: "BLs");

            migrationBuilder.DropForeignKey(
                name: "FK_BLs_PaymentTypes_MasterPaymentTypeId",
                schema: "ops",
                table: "BLs");

            migrationBuilder.DropForeignKey(
                name: "FK_BLs_Users_CreateUserId",
                schema: "ops",
                table: "BLs");

            migrationBuilder.DropForeignKey(
                name: "FK_BLs_Users_EditUserId",
                schema: "ops",
                table: "BLs");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_ServiceRequests_ServiceRequestId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.DropTable(
                name: "ServiceRequestPaymentDocuments",
                schema: "req");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_ServiceRequestId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_BLs_CreateUserId",
                schema: "ops",
                table: "BLs");

            migrationBuilder.DropIndex(
                name: "IX_BLs_EditUserId",
                schema: "ops",
                table: "BLs");

            migrationBuilder.DropIndex(
                name: "IX_BLs_MasterBLReleaseTypeId",
                schema: "ops",
                table: "BLs");

            migrationBuilder.DropIndex(
                name: "IX_BLs_MasterPaymentTypeId",
                schema: "ops",
                table: "BLs");

            migrationBuilder.DeleteData(
                schema: "base",
                table: "ServiceRequestTypes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "ServiceRequestId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "LastStatusDate",
                schema: "fin",
                table: "Cheques");
        }
    }
}
