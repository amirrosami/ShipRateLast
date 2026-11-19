using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T41 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CarrierId",
                schema: "ops",
                table: "LegDispatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegDispatches_CarrierId",
                schema: "ops",
                table: "LegDispatches",
                column: "CarrierId");

            migrationBuilder.AddForeignKey(
                name: "FK_LegDispatches_Carriers_CarrierId",
                schema: "ops",
                table: "LegDispatches",
                column: "CarrierId",
                principalSchema: "ops",
                principalTable: "Carriers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LegDispatches_Carriers_CarrierId",
                schema: "ops",
                table: "LegDispatches");

            migrationBuilder.DropIndex(
                name: "IX_LegDispatches_CarrierId",
                schema: "ops",
                table: "LegDispatches");

            migrationBuilder.DropColumn(
                name: "CarrierId",
                schema: "ops",
                table: "LegDispatches");
        }
    }
}
