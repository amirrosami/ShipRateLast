using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T37 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DOChecklistDefinitions",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckListName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CheckListLocalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOChecklistDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DOProcesses",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BLId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LawyerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerRequestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LineDODate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BLReleaseTypeId = table.Column<int>(type: "int", nullable: true),
                    CSControllerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CSControllerComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CSControllerDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CSControllerStatus = table.Column<int>(type: "int", nullable: false),
                    FinanceControllerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinanceControllerComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FinanceControllerDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinanceControllerStatus = table.Column<int>(type: "int", nullable: false),
                    FinalControllerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinalControllerComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FinalControllerDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalControllerStatus = table.Column<int>(type: "int", nullable: false),
                    SettlementExceptionApprovedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettlementExceptionApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettlementExceptionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DODeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DODeliverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_DOProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DOProcesses_BLReleaseTypes_BLReleaseTypeId",
                        column: x => x.BLReleaseTypeId,
                        principalSchema: "base",
                        principalTable: "BLReleaseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcesses_BLs_BLId",
                        column: x => x.BLId,
                        principalSchema: "ops",
                        principalTable: "BLs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcesses_Persons_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "base",
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcesses_Users_CSControllerId",
                        column: x => x.CSControllerId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcesses_Users_FinalControllerId",
                        column: x => x.FinalControllerId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcesses_Users_FinanceControllerId",
                        column: x => x.FinanceControllerId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcesses_Users_SettlementExceptionApprovedById",
                        column: x => x.SettlementExceptionApprovedById,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOProcessChecklistItems",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DOProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistDefinitionId = table.Column<int>(type: "int", nullable: false),
                    IsChecked = table.Column<bool>(type: "bit", nullable: false),
                    CheckedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_DOProcessChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DOProcessChecklistItems_DOChecklistDefinitions_ChecklistDefinitionId",
                        column: x => x.ChecklistDefinitionId,
                        principalSchema: "ops",
                        principalTable: "DOChecklistDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcessChecklistItems_DOProcesses_DOProcessId",
                        column: x => x.DOProcessId,
                        principalSchema: "ops",
                        principalTable: "DOProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcessChecklistItems_Users_CheckedByUserId",
                        column: x => x.CheckedByUserId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DOProcessCustomerDebtSnapshots",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DOProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DetentionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OnlineDetentionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_DOProcessCustomerDebtSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DOProcessCustomerDebtSnapshots_Currency_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "base",
                        principalTable: "Currency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcessCustomerDebtSnapshots_DOProcesses_DOProcessId",
                        column: x => x.DOProcessId,
                        principalSchema: "ops",
                        principalTable: "DOProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcessCustomerDebtSnapshots_Detentions_DetentionId",
                        column: x => x.DetentionId,
                        principalSchema: "cnt",
                        principalTable: "Detentions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcessCustomerDebtSnapshots_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "fin",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcessCustomerDebtSnapshots_OnlineDetentions_OnlineDetentionId",
                        column: x => x.OnlineDetentionId,
                        principalSchema: "cnt",
                        principalTable: "OnlineDetentions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcessCustomerDebtSnapshots_Persons_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "base",
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DOProcessCustomerDebtSnapshots_Users_SnapshotByUserId",
                        column: x => x.SnapshotByUserId,
                        principalSchema: "sec",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DOChecklistDefinitions_CheckListName",
                schema: "ops",
                table: "DOChecklistDefinitions",
                column: "CheckListName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessChecklistItems_CheckedByUserId",
                schema: "ops",
                table: "DOProcessChecklistItems",
                column: "CheckedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessChecklistItems_ChecklistDefinitionId",
                schema: "ops",
                table: "DOProcessChecklistItems",
                column: "ChecklistDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessChecklistItems_DOProcessId_ChecklistDefinitionId",
                schema: "ops",
                table: "DOProcessChecklistItems",
                columns: new[] { "DOProcessId", "ChecklistDefinitionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessCustomerDebtSnapshots_CurrencyId",
                schema: "ops",
                table: "DOProcessCustomerDebtSnapshots",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessCustomerDebtSnapshots_CustomerId",
                schema: "ops",
                table: "DOProcessCustomerDebtSnapshots",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessCustomerDebtSnapshots_DetentionId",
                schema: "ops",
                table: "DOProcessCustomerDebtSnapshots",
                column: "DetentionId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessCustomerDebtSnapshots_DOProcessId_CustomerId",
                schema: "ops",
                table: "DOProcessCustomerDebtSnapshots",
                columns: new[] { "DOProcessId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessCustomerDebtSnapshots_InvoiceId",
                schema: "ops",
                table: "DOProcessCustomerDebtSnapshots",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessCustomerDebtSnapshots_OnlineDetentionId",
                schema: "ops",
                table: "DOProcessCustomerDebtSnapshots",
                column: "OnlineDetentionId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcessCustomerDebtSnapshots_SnapshotByUserId",
                schema: "ops",
                table: "DOProcessCustomerDebtSnapshots",
                column: "SnapshotByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcesses_BLId",
                schema: "ops",
                table: "DOProcesses",
                column: "BLId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcesses_BLReleaseTypeId",
                schema: "ops",
                table: "DOProcesses",
                column: "BLReleaseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcesses_CSControllerId",
                schema: "ops",
                table: "DOProcesses",
                column: "CSControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcesses_CustomerId",
                schema: "ops",
                table: "DOProcesses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcesses_FinalControllerId",
                schema: "ops",
                table: "DOProcesses",
                column: "FinalControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcesses_FinanceControllerId",
                schema: "ops",
                table: "DOProcesses",
                column: "FinanceControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_DOProcesses_SettlementExceptionApprovedById",
                schema: "ops",
                table: "DOProcesses",
                column: "SettlementExceptionApprovedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DOProcessChecklistItems",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "DOProcessCustomerDebtSnapshots",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "DOChecklistDefinitions",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "DOProcesses",
                schema: "ops");
        }
    }
}
