using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T29 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TCTSCode",
                schema: "base",
                table: "TradeTypes",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TCTSCode",
                schema: "base",
                table: "LoadTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TCTSCode",
                schema: "base",
                table: "FullMT",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TCTSCode",
                schema: "base",
                table: "ContainerTypes",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "ContainerTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "FullMT",
                keyColumn: "Id",
                keyValue: 4,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "FullMT",
                keyColumn: "Id",
                keyValue: 5,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "LoadTypes",
                keyColumn: "Id",
                keyValue: 99,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 20,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 21,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 22,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 23,
                column: "TCTSCode",
                value: null);

            migrationBuilder.UpdateData(
                schema: "base",
                table: "TradeTypes",
                keyColumn: "Id",
                keyValue: 24,
                column: "TCTSCode",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_ContainerActions_CreateUserId",
                schema: "cnt",
                table: "ContainerActions",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerActions_DeleteUserId",
                schema: "cnt",
                table: "ContainerActions",
                column: "DeleteUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerActions_EditUserId",
                schema: "cnt",
                table: "ContainerActions",
                column: "EditUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContainerActions_Users_CreateUserId",
                schema: "cnt",
                table: "ContainerActions",
                column: "CreateUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContainerActions_Users_DeleteUserId",
                schema: "cnt",
                table: "ContainerActions",
                column: "DeleteUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContainerActions_Users_EditUserId",
                schema: "cnt",
                table: "ContainerActions",
                column: "EditUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContainerActions_Users_CreateUserId",
                schema: "cnt",
                table: "ContainerActions");

            migrationBuilder.DropForeignKey(
                name: "FK_ContainerActions_Users_DeleteUserId",
                schema: "cnt",
                table: "ContainerActions");

            migrationBuilder.DropForeignKey(
                name: "FK_ContainerActions_Users_EditUserId",
                schema: "cnt",
                table: "ContainerActions");

            migrationBuilder.DropIndex(
                name: "IX_ContainerActions_CreateUserId",
                schema: "cnt",
                table: "ContainerActions");

            migrationBuilder.DropIndex(
                name: "IX_ContainerActions_DeleteUserId",
                schema: "cnt",
                table: "ContainerActions");

            migrationBuilder.DropIndex(
                name: "IX_ContainerActions_EditUserId",
                schema: "cnt",
                table: "ContainerActions");

            migrationBuilder.DropColumn(
                name: "TCTSCode",
                schema: "base",
                table: "TradeTypes");

            migrationBuilder.DropColumn(
                name: "TCTSCode",
                schema: "base",
                table: "LoadTypes");

            migrationBuilder.DropColumn(
                name: "TCTSCode",
                schema: "base",
                table: "FullMT");

            migrationBuilder.DropColumn(
                name: "TCTSCode",
                schema: "base",
                table: "ContainerTypes");
        }
    }
}
