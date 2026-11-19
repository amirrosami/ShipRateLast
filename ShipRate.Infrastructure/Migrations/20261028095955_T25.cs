using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T25 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlackLists_Persons_PersonId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.DropIndex(
                name: "IX_BlackLists_PersonId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.RenameColumn(
                name: "PersonId",
                schema: "crm",
                table: "BlackLists",
                newName: "EntityId");

            migrationBuilder.AddColumn<string>(
                name: "EntityReference",
                schema: "crm",
                table: "BlackLists",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntityTypeId",
                schema: "crm",
                table: "BlackLists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                schema: "base",
                table: "SystemObjectTypes",
                columns: new[] { "Id", "IsActive", "IsDeleted", "SystemObjectTypeLocalName", "SystemObjectTypeName" },
                values: new object[] { 30, true, false, "لیست سیاه", "BlackLists" });

            migrationBuilder.CreateIndex(
                name: "IX_BlackLists_BlackListEnterUserId",
                schema: "crm",
                table: "BlackLists",
                column: "BlackListEnterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlackLists_BlackListExitUserId",
                schema: "crm",
                table: "BlackLists",
                column: "BlackListExitUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlackLists_EntityTypeId",
                schema: "crm",
                table: "BlackLists",
                column: "EntityTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlackLists_EntityTypes_EntityTypeId",
                schema: "crm",
                table: "BlackLists",
                column: "EntityTypeId",
                principalSchema: "base",
                principalTable: "EntityTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BlackLists_Users_BlackListEnterUserId",
                schema: "crm",
                table: "BlackLists",
                column: "BlackListEnterUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BlackLists_Users_BlackListExitUserId",
                schema: "crm",
                table: "BlackLists",
                column: "BlackListExitUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlackLists_EntityTypes_EntityTypeId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.DropForeignKey(
                name: "FK_BlackLists_Users_BlackListEnterUserId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.DropForeignKey(
                name: "FK_BlackLists_Users_BlackListExitUserId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.DropIndex(
                name: "IX_BlackLists_BlackListEnterUserId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.DropIndex(
                name: "IX_BlackLists_BlackListExitUserId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.DropIndex(
                name: "IX_BlackLists_EntityTypeId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.DeleteData(
                schema: "base",
                table: "SystemObjectTypes",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DropColumn(
                name: "EntityReference",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.DropColumn(
                name: "EntityTypeId",
                schema: "crm",
                table: "BlackLists");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                schema: "crm",
                table: "BlackLists",
                newName: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_BlackLists_PersonId",
                schema: "crm",
                table: "BlackLists",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlackLists_Persons_PersonId",
                schema: "crm",
                table: "BlackLists",
                column: "PersonId",
                principalSchema: "base",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
