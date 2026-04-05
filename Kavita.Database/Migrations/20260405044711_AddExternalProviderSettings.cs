using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kavita.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalProviderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableGoogleBooks",
                table: "MetadataSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableHardcover",
                table: "MetadataSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableOpenLibrary",
                table: "MetadataSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GoogleBooksApiKey",
                table: "MetadataSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HardcoverApiKey",
                table: "MetadataSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableGoogleBooks",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "EnableHardcover",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "EnableOpenLibrary",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "GoogleBooksApiKey",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "HardcoverApiKey",
                table: "MetadataSettings");
        }
    }
}
