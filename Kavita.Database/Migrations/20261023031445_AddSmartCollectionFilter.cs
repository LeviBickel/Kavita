using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kavita.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartCollectionFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SmartFilterId",
                table: "AppUserCollection",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserCollection_SmartFilterId",
                table: "AppUserCollection",
                column: "SmartFilterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUserCollection_AppUserSmartFilter_SmartFilterId",
                table: "AppUserCollection",
                column: "SmartFilterId",
                principalTable: "AppUserSmartFilter",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUserCollection_AppUserSmartFilter_SmartFilterId",
                table: "AppUserCollection");

            migrationBuilder.DropIndex(
                name: "IX_AppUserCollection_SmartFilterId",
                table: "AppUserCollection");

            migrationBuilder.DropColumn(
                name: "SmartFilterId",
                table: "AppUserCollection");
        }
    }
}
