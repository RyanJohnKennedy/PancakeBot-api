using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PancakeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateTotdResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TotdResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MapUid = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TotdResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TotdResults_Players_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Players",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TotdResults_TotdMaps_MapUid",
                        column: x => x.MapUid,
                        principalTable: "TotdMaps",
                        principalColumn: "MapUid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TotdResults_AccountId",
                table: "TotdResults",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TotdResults_MapUid_AccountId",
                table: "TotdResults",
                columns: new[] { "MapUid", "AccountId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TotdResults");
        }
    }
}
