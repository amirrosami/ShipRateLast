using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                schema: "cnt",
                table: "OnlineDetentions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EarliestFreeTimeExpiryDate",
                schema: "cnt",
                table: "OnlineDetentions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCalculatedDate",
                schema: "cnt",
                table: "OnlineDetentions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TotalContainersCount",
                schema: "cnt",
                table: "OnlineDetentions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnreturnedContainersCount",
                schema: "cnt",
                table: "OnlineDetentions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OnlineDetentionItems",
                schema: "cnt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OnlineDetentionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContainerNo = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ContainerSizeTypeCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DischargeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FreeTimeDays = table.Column<int>(type: "int", nullable: false),
                    FreeTimeExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmptyReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OverdueDays = table.Column<int>(type: "int", nullable: false),
                    CurrentDailyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItemGross = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItemVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItemNet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_OnlineDetentionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnlineDetentionItems_OnlineDetentions_OnlineDetentionId",
                        column: x => x.OnlineDetentionId,
                        principalSchema: "cnt",
                        principalTable: "OnlineDetentions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineDetentions_CurrencyId",
                schema: "cnt",
                table: "OnlineDetentions",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_OnlineDetentionItems_OnlineDetentionId",
                schema: "cnt",
                table: "OnlineDetentionItems",
                column: "OnlineDetentionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OnlineDetentions_Currency_CurrencyId",
                schema: "cnt",
                table: "OnlineDetentions",
                column: "CurrencyId",
                principalSchema: "base",
                principalTable: "Currency",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OnlineDetentions_Currency_CurrencyId",
                schema: "cnt",
                table: "OnlineDetentions");

            migrationBuilder.DropTable(
                name: "OnlineDetentionItems",
                schema: "cnt");

            migrationBuilder.DropIndex(
                name: "IX_OnlineDetentions_CurrencyId",
                schema: "cnt",
                table: "OnlineDetentions");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                schema: "cnt",
                table: "OnlineDetentions");

            migrationBuilder.DropColumn(
                name: "EarliestFreeTimeExpiryDate",
                schema: "cnt",
                table: "OnlineDetentions");

            migrationBuilder.DropColumn(
                name: "LastCalculatedDate",
                schema: "cnt",
                table: "OnlineDetentions");

            migrationBuilder.DropColumn(
                name: "TotalContainersCount",
                schema: "cnt",
                table: "OnlineDetentions");

            migrationBuilder.DropColumn(
                name: "UnreturnedContainersCount",
                schema: "cnt",
                table: "OnlineDetentions");
        }
    }
}
