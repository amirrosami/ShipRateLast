using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T39 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegDispatchStatus",
                schema: "base",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegDispatchStatusName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LegDispatchStatusLocalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegDispatchStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegDispatches",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentLegId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TruckPlate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DriverPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WaybillNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlannedDispatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDispatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActulaArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LegDispatchStatusId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_LegDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegDispatches_LegDispatchStatus_LegDispatchStatusId",
                        column: x => x.LegDispatchStatusId,
                        principalSchema: "base",
                        principalTable: "LegDispatchStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegDispatches_ShipmentLegs_ShipmentLegId",
                        column: x => x.ShipmentLegId,
                        principalSchema: "ops",
                        principalTable: "ShipmentLegs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "fin",
                table: "InvoiceSubjects",
                columns: new[] { "Id", "InvoiceSubjectLocalName", "InvoiceSubjectName", "IsActive", "IsDeleted" },
                values: new object[] { 80, "صورتحساب کمیسیون فروشنده", "Commission Invoice", true, false });

            migrationBuilder.InsertData(
                schema: "base",
                table: "LegDispatchStatus",
                columns: new[] { "Id", "IsActive", "IsDeleted", "LegDispatchStatusLocalName", "LegDispatchStatusName", "SortOrder" },
                values: new object[,]
                {
                    { 10, true, false, "برنامه‌ریزی‌شده", "Planned", 1 },
                    { 15, true, false, "کامیون/وسیله تخصیص داده شده", "Assigned", 2 },
                    { 20, true, false, "در حال بارگیری", "Loading", 3 },
                    { 25, true, false, "بارگیری‌شده", "Loaded", 4 },
                    { 30, true, false, "حرکت کرده", "Dispatched", 5 },
                    { 35, true, false, "در مسیر", "InTransit", 6 },
                    { 40, true, false, "رسیده به مقصد", "Arrived", 7 },
                    { 45, true, false, "در حال تخلیه", "Unloading", 8 },
                    { 50, true, false, "تحویل‌شده", "Delivered", 9 },
                    { 55, true, false, "لغو شده", "Cancelled", 10 },
                    { 99, true, false, "متوقف / در انتظار", "OnHold", 11 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegDispatches_LegDispatchStatusId",
                schema: "ops",
                table: "LegDispatches",
                column: "LegDispatchStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_LegDispatches_ShipmentLegId",
                schema: "ops",
                table: "LegDispatches",
                column: "ShipmentLegId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegDispatches",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "LegDispatchStatus",
                schema: "base");

            migrationBuilder.DeleteData(
                schema: "fin",
                table: "InvoiceSubjects",
                keyColumn: "Id",
                keyValue: 80);
        }
    }
}
