using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentRequestCategory",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentRequestCategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentRequestCategoryLocalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRequestCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRequestStatus",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentRequestStatusName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentRequestStatusLocalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRequestStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRequests",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestNo = table.Column<long>(type: "bigint", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinanceDirectionId = table.Column<int>(type: "int", nullable: false),
                    PaymentRequestCategoryId = table.Column<int>(type: "int", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    PreferredPaymentMethodId = table.Column<int>(type: "int", nullable: false),
                    TargetIban = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetAccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetBankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CashPaymentRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalConfirmedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinanlConfirmDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_PaymentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "base",
                        principalTable: "Branches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_CashPaymentRequests_CashPaymentRequestId",
                        column: x => x.CashPaymentRequestId,
                        principalSchema: "fin",
                        principalTable: "CashPaymentRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "cmp",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Currency_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "base",
                        principalTable: "Currency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_PaymentDocuments_PaymentDocumentId",
                        column: x => x.PaymentDocumentId,
                        principalSchema: "fin",
                        principalTable: "PaymentDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_PaymentMethods_PreferredPaymentMethodId",
                        column: x => x.PreferredPaymentMethodId,
                        principalSchema: "base",
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_PaymentRequestCategory_PaymentRequestCategoryId",
                        column: x => x.PaymentRequestCategoryId,
                        principalSchema: "fin",
                        principalTable: "PaymentRequestCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_PaymentRequestStatus_StatusId",
                        column: x => x.StatusId,
                        principalSchema: "fin",
                        principalTable: "PaymentRequestStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Persons_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "base",
                        principalTable: "Persons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalSchema: "fin",
                        principalTable: "Settlements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Users_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Users_FinalConfirmedByUserId",
                        column: x => x.FinalConfirmedByUserId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Users_RejectedByUserId",
                        column: x => x.RejectedByUserId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentRequestItems",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourcePaymentDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DetentionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PaymentRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRequestItems_Detentions_DetentionId",
                        column: x => x.DetentionId,
                        principalSchema: "cnt",
                        principalTable: "Detentions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequestItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "fin",
                        principalTable: "Invoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequestItems_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "ops",
                        principalTable: "Jobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequestItems_PaymentDocuments_SourcePaymentDocumentId",
                        column: x => x.SourcePaymentDocumentId,
                        principalSchema: "fin",
                        principalTable: "PaymentDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentRequestItems_PaymentRequests_PaymentRequestId",
                        column: x => x.PaymentRequestId,
                        principalSchema: "fin",
                        principalTable: "PaymentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "fin",
                table: "PaymentRequestCategory",
                columns: new[] { "Id", "IsActive", "IsDeleted", "PaymentRequestCategoryLocalName", "PaymentRequestCategoryName" },
                values: new object[,]
                {
                    { 1, true, false, "عودت طلب/سپرده مشتری", "عودت طلب/سپرده مشتری" },
                    { 2, true, false, "فاکتور خرید", "فاکتور خرید" },
                    { 3, true, false, "شارژ تنخواه", "شارژ تنخواه" },
                    { 4, true, false, "هزینه مستقیم", "هزینه مستقیم" }
                });

            migrationBuilder.InsertData(
                schema: "fin",
                table: "PaymentRequestStatus",
                columns: new[] { "Id", "IsActive", "IsDeleted", "PaymentRequestStatusLocalName", "PaymentRequestStatusName" },
                values: new object[,]
                {
                    { 1, true, false, "درافت /درخواست اولیه", "Draft" },
                    { 2, true, false, "تایید میانی شده", "Confirmed" },
                    { 3, true, false, "تایید نهایی شده", "Approved" },
                    { 4, true, false, "پرداخت شده", "Paid" },
                    { 5, true, false, "رد شده", "Rejected" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequestItems_DetentionId",
                schema: "fin",
                table: "PaymentRequestItems",
                column: "DetentionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequestItems_InvoiceId",
                schema: "fin",
                table: "PaymentRequestItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequestItems_JobId",
                schema: "fin",
                table: "PaymentRequestItems",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequestItems_PaymentRequestId",
                schema: "fin",
                table: "PaymentRequestItems",
                column: "PaymentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequestItems_SourcePaymentDocumentId",
                schema: "fin",
                table: "PaymentRequestItems",
                column: "SourcePaymentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ApprovedByUserId",
                schema: "fin",
                table: "PaymentRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_BranchId",
                schema: "fin",
                table: "PaymentRequests",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_CashPaymentRequestId",
                schema: "fin",
                table: "PaymentRequests",
                column: "CashPaymentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_CompanyId",
                schema: "fin",
                table: "PaymentRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_CurrencyId",
                schema: "fin",
                table: "PaymentRequests",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_ExecutedByUserId",
                schema: "fin",
                table: "PaymentRequests",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_FinalConfirmedByUserId",
                schema: "fin",
                table: "PaymentRequests",
                column: "FinalConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_PartyId",
                schema: "fin",
                table: "PaymentRequests",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_PaymentDocumentId",
                schema: "fin",
                table: "PaymentRequests",
                column: "PaymentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_PaymentRequestCategoryId",
                schema: "fin",
                table: "PaymentRequests",
                column: "PaymentRequestCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_PreferredPaymentMethodId",
                schema: "fin",
                table: "PaymentRequests",
                column: "PreferredPaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_RejectedByUserId",
                schema: "fin",
                table: "PaymentRequests",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_RequestedByUserId",
                schema: "fin",
                table: "PaymentRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_SettlementId",
                schema: "fin",
                table: "PaymentRequests",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_StatusId",
                schema: "fin",
                table: "PaymentRequests",
                column: "StatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRequestItems",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "PaymentRequests",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "PaymentRequestCategory",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "PaymentRequestStatus",
                schema: "fin");
        }
    }
}
