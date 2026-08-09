using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PancakeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTotdMapMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampaignUid",
                table: "TotdMaps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapName",
                table: "TotdMaps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeasonUid",
                table: "TotdMaps",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampaignUid",
                table: "TotdMaps");

            migrationBuilder.DropColumn(
                name: "MapName",
                table: "TotdMaps");

            migrationBuilder.DropColumn(
                name: "SeasonUid",
                table: "TotdMaps");
        }
    }
}
