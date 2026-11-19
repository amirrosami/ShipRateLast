using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T35 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FACContracts_Currency_CurrencyId",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_FACContracts_Customers_ForwarderId",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropIndex(
                name: "IX_FACContracts_CompanyId",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.RenameColumn(
                name: "FACPercent",
                schema: "ops",
                table: "FACContracts",
                newName: "MinimumFACAmount");

            migrationBuilder.RenameColumn(
                name: "FACPerTEU",
                schema: "ops",
                table: "FACContracts",
                newName: "MaximumFACAmount");

            migrationBuilder.RenameColumn(
                name: "FACPerCBM",
                schema: "ops",
                table: "FACContracts",
                newName: "DefaultRate");

            migrationBuilder.AlterColumn<Guid>(
                name: "ForwarderId",
                schema: "ops",
                table: "FACContracts",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "FacContractRemarks",
                schema: "ops",
                table: "FACContracts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "CarrierId",
                schema: "ops",
                table: "FACContracts",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ContractNo",
                schema: "ops",
                table: "FACContracts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CounterpartyRole",
                schema: "ops",
                table: "FACContracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultCalculationBasis",
                schema: "ops",
                table: "FACContracts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Direction",
                schema: "ops",
                table: "FACContracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "ops",
                table: "FACContracts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedDate",
                schema: "ops",
                table: "FACContracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "ops",
                table: "FACContracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FACContractRateLines",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FACContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    POLId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PODId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransportTypeId = table.Column<int>(type: "int", nullable: true),
                    LoadTypeId = table.Column<int>(type: "int", nullable: true),
                    ContainerSizeTypeId = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    CalculationBasis = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    MinimumFACAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaximumFACAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    POLFreeDays = table.Column<int>(type: "int", nullable: true),
                    PODFreeDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_FACContractRateLines", x => x.Id);
                    table.CheckConstraint("CK_FACRateLines_AmountLimits", "([MinimumFACAmount] IS NULL OR [MinimumFACAmount] >= 0) AND ([MaximumFACAmount] IS NULL OR [MaximumFACAmount] >= 0) AND ([MinimumFACAmount] IS NULL OR [MaximumFACAmount] IS NULL OR [MinimumFACAmount] <= [MaximumFACAmount])");
                    table.CheckConstraint("CK_FACRateLines_Dates", "[ValidFrom] IS NULL OR [ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.CheckConstraint("CK_FACRateLines_FreeDays", "([POLFreeDays] IS NULL OR [POLFreeDays] >= 0) AND ([PODFreeDays] IS NULL OR [PODFreeDays] >= 0)");
                    table.CheckConstraint("CK_FACRateLines_Rate", "[Rate] >= 0");
                    table.ForeignKey(
                        name: "FK_FACContractRateLines_ContainerSizeTypes_ContainerSizeTypeId",
                        column: x => x.ContainerSizeTypeId,
                        principalSchema: "base",
                        principalTable: "ContainerSizeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FACContractRateLines_Currency_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "base",
                        principalTable: "Currency",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FACContractRateLines_FACContracts_FACContractId",
                        column: x => x.FACContractId,
                        principalSchema: "ops",
                        principalTable: "FACContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FACContractRateLines_LoadTypes_LoadTypeId",
                        column: x => x.LoadTypeId,
                        principalSchema: "base",
                        principalTable: "LoadTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FACContractRateLines_Locations_PODId",
                        column: x => x.PODId,
                        principalSchema: "base",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FACContractRateLines_Locations_POLId",
                        column: x => x.POLId,
                        principalSchema: "base",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FACContractRateLines_TransportTypes_TransportTypeId",
                        column: x => x.TransportTypeId,
                        principalSchema: "base",
                        principalTable: "TransportTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 301905,
                column: "DisplayName",
                value: "Initial Confirm Vault Movements");

            migrationBuilder.UpdateData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 301910,
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Final Confirm Vault Movements", "Finance.VaultMovements.FinalConfirm" });

            migrationBuilder.CreateIndex(
                name: "IX_FACContracts_CompanyId_ContractNo",
                schema: "ops",
                table: "FACContracts",
                columns: new[] { "CompanyId", "ContractNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FACContracts_CompanyId_Direction_Status_StartDate_EndDate",
                schema: "ops",
                table: "FACContracts",
                columns: new[] { "CompanyId", "Direction", "Status", "StartDate", "EndDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_FACContracts_AmountLimits",
                schema: "ops",
                table: "FACContracts",
                sql: "([MinimumFACAmount] IS NULL OR [MinimumFACAmount] >= 0) AND ([MaximumFACAmount] IS NULL OR [MaximumFACAmount] >= 0) AND ([MinimumFACAmount] IS NULL OR [MaximumFACAmount] IS NULL OR [MinimumFACAmount] <= [MaximumFACAmount])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FACContracts_Counterparty",
                schema: "ops",
                table: "FACContracts",
                sql: "([CounterpartyRole] = 1 AND [CarrierId] IS NOT NULL) OR ([CounterpartyRole] = 2 AND [ForwarderId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FACContracts_Dates",
                schema: "ops",
                table: "FACContracts",
                sql: "[EndDate] >= [StartDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FACContracts_DefaultRate",
                schema: "ops",
                table: "FACContracts",
                sql: "([DefaultCalculationBasis] IS NULL AND [DefaultRate] IS NULL) OR ([DefaultCalculationBasis] IS NOT NULL AND [DefaultRate] IS NOT NULL AND [DefaultRate] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FACContracts_FreeDays",
                schema: "ops",
                table: "FACContracts",
                sql: "([POLFreeDays] IS NULL OR [POLFreeDays] >= 0) AND ([PODFreeDays] IS NULL OR [PODFreeDays] >= 0)");

            migrationBuilder.CreateIndex(
                name: "IX_FACContractRateLines_ContainerSizeTypeId",
                schema: "ops",
                table: "FACContractRateLines",
                column: "ContainerSizeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FACContractRateLines_CurrencyId",
                schema: "ops",
                table: "FACContractRateLines",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FACContractRateLines_FACContractId_IsActive_Priority",
                schema: "ops",
                table: "FACContractRateLines",
                columns: new[] { "FACContractId", "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_FACContractRateLines_LoadTypeId",
                schema: "ops",
                table: "FACContractRateLines",
                column: "LoadTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FACContractRateLines_PODId",
                schema: "ops",
                table: "FACContractRateLines",
                column: "PODId");

            migrationBuilder.CreateIndex(
                name: "IX_FACContractRateLines_POLId_PODId_TransportTypeId_LoadTypeId_ContainerSizeTypeId",
                schema: "ops",
                table: "FACContractRateLines",
                columns: new[] { "POLId", "PODId", "TransportTypeId", "LoadTypeId", "ContainerSizeTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_FACContractRateLines_TransportTypeId",
                schema: "ops",
                table: "FACContractRateLines",
                column: "TransportTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_FACContracts_Currency_CurrencyId",
                schema: "ops",
                table: "FACContracts",
                column: "CurrencyId",
                principalSchema: "base",
                principalTable: "Currency",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FACContracts_Persons_ForwarderId",
                schema: "ops",
                table: "FACContracts",
                column: "ForwarderId",
                principalSchema: "base",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FACContracts_Currency_CurrencyId",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_FACContracts_Persons_ForwarderId",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropTable(
                name: "FACContractRateLines",
                schema: "ops");

            migrationBuilder.DropIndex(
                name: "IX_FACContracts_CompanyId_ContractNo",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropIndex(
                name: "IX_FACContracts_CompanyId_Direction_Status_StartDate_EndDate",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FACContracts_AmountLimits",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FACContracts_Counterparty",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FACContracts_Dates",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FACContracts_DefaultRate",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FACContracts_FreeDays",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropColumn(
                name: "ContractNo",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropColumn(
                name: "CounterpartyRole",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropColumn(
                name: "DefaultCalculationBasis",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropColumn(
                name: "Direction",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropColumn(
                name: "SignedDate",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "ops",
                table: "FACContracts");

            migrationBuilder.RenameColumn(
                name: "MinimumFACAmount",
                schema: "ops",
                table: "FACContracts",
                newName: "FACPercent");

            migrationBuilder.RenameColumn(
                name: "MaximumFACAmount",
                schema: "ops",
                table: "FACContracts",
                newName: "FACPerTEU");

            migrationBuilder.RenameColumn(
                name: "DefaultRate",
                schema: "ops",
                table: "FACContracts",
                newName: "FACPerCBM");

            migrationBuilder.AlterColumn<Guid>(
                name: "ForwarderId",
                schema: "ops",
                table: "FACContracts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FacContractRemarks",
                schema: "ops",
                table: "FACContracts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CarrierId",
                schema: "ops",
                table: "FACContracts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.UpdateData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 301905,
                column: "DisplayName",
                value: "First Confirm Vault Movements");

            migrationBuilder.UpdateData(
                schema: "sec",
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 301910,
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Authorize Vault Movements", "Finance.VaultMovements.Authorize" });

            migrationBuilder.CreateIndex(
                name: "IX_FACContracts_CompanyId",
                schema: "ops",
                table: "FACContracts",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_FACContracts_Currency_CurrencyId",
                schema: "ops",
                table: "FACContracts",
                column: "CurrencyId",
                principalSchema: "base",
                principalTable: "Currency",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FACContracts_Customers_ForwarderId",
                schema: "ops",
                table: "FACContracts",
                column: "ForwarderId",
                principalSchema: "crm",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
