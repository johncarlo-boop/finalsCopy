using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyInventory.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureAndPasswordChangeRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePicturePath",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPasswordChange",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicturePath",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequiresPasswordChange",
                table: "Users");
        }
    }
}
