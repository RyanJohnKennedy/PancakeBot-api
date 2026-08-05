using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PancakeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlayerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Pancakes",
                table: "Pancakes");

            migrationBuilder.RenameTable(
                name: "Pancakes",
                newName: "Players");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Players",
                table: "Players",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Players",
                table: "Players");

            migrationBuilder.RenameTable(
                name: "Players",
                newName: "Pancakes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pancakes",
                table: "Pancakes",
                column: "Id");
        }
    }
}
