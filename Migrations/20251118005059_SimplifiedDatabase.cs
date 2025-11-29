using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyInventory.Migrations
{
    /// <inheritdoc />
    public partial class SimplifiedDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Properties table already exists, skip creation
            // Only create Users table if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `Users` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `Email` varchar(256) CHARACTER SET utf8mb4 NOT NULL,
                    `PasswordHash` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                    `FullName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                    `IsAdmin` tinyint(1) NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
                ) CHARACTER SET=utf8mb4;

                CREATE UNIQUE INDEX IF NOT EXISTS `IX_Users_Email` ON `Users` (`Email`);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
