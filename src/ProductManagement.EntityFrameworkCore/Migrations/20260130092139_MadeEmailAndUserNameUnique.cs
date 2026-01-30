using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagement.Migrations
{
    /// <inheritdoc />
    public partial class MadeEmailAndUserNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_TenantId_Email",
                table: "AbpUsers",
                columns: new[] { "TenantId", "Email" },
                unique: true,
                filter: "\"IsDeleted\" = 'false'");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_TenantId_NormalizedEmail",
                table: "AbpUsers",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true,
                filter: "\"IsDeleted\" = 'false'");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_TenantId_NormalizedUserName",
                table: "AbpUsers",
                columns: new[] { "TenantId", "NormalizedUserName" },
                unique: true,
                filter: "\"IsDeleted\" = 'false'");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_TenantId_UserName",
                table: "AbpUsers",
                columns: new[] { "TenantId", "UserName" },
                unique: true,
                filter: "\"IsDeleted\" = 'false'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AbpUsers_TenantId_Email",
                table: "AbpUsers");

            migrationBuilder.DropIndex(
                name: "IX_AbpUsers_TenantId_NormalizedEmail",
                table: "AbpUsers");

            migrationBuilder.DropIndex(
                name: "IX_AbpUsers_TenantId_NormalizedUserName",
                table: "AbpUsers");

            migrationBuilder.DropIndex(
                name: "IX_AbpUsers_TenantId_UserName",
                table: "AbpUsers");
        }
    }
}
