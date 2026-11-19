using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T34 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "RequestSentDate",
                schema: "fin",
                table: "PaymentRequests",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "VaultId",
                schema: "fin",
                table: "PaymentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRequests_VaultId",
                schema: "fin",
                table: "PaymentRequests",
                column: "VaultId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentRequests_Vaults_VaultId",
                schema: "fin",
                table: "PaymentRequests",
                column: "VaultId",
                principalSchema: "fin",
                principalTable: "Vaults",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentRequests_Vaults_VaultId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.DropIndex(
                name: "IX_PaymentRequests_VaultId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.DropColumn(
                name: "VaultId",
                schema: "fin",
                table: "PaymentRequests");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RequestSentDate",
                schema: "fin",
                table: "PaymentRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
