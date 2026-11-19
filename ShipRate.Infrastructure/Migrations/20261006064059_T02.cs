using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipRate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class T02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_PermissionId_UserId",
                schema: "sec",
                table: "UserPermissions");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId_UserId_IsDeleted",
                schema: "sec",
                table: "UserPermissions",
                columns: new[] { "PermissionId", "UserId", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_CreateUserId",
                schema: "base",
                table: "Persons",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_EditUserId",
                schema: "base",
                table: "Persons",
                column: "EditUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_Users_CreateUserId",
                schema: "base",
                table: "Persons",
                column: "CreateUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_Users_EditUserId",
                schema: "base",
                table: "Persons",
                column: "EditUserId",
                principalSchema: "sec",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Persons_Users_CreateUserId",
                schema: "base",
                table: "Persons");

            migrationBuilder.DropForeignKey(
                name: "FK_Persons_Users_EditUserId",
                schema: "base",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_PermissionId_UserId_IsDeleted",
                schema: "sec",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Persons_CreateUserId",
                schema: "base",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_Persons_EditUserId",
                schema: "base",
                table: "Persons");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId_UserId",
                schema: "sec",
                table: "UserPermissions",
                columns: new[] { "PermissionId", "UserId" },
                unique: true);
        }
    }
}
