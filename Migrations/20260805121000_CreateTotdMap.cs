using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PancakeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateTotdMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TotdMaps",
                columns: table => new
                {
                    MapUid = table.Column<string>(type: "text", nullable: false),
                    TotdDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    StartTimestamp = table.Column<long>(type: "bigint", nullable: false),
                    EndTimestamp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TotdMaps", x => x.MapUid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TotdMaps");
        }
    }
}
