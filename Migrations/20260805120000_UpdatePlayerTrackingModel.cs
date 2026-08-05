using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PancakeBot.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlayerTrackingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Players",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "Players",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Players",
                newName: "AccountId");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Players",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZoneId",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZoneName",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Players",
                table: "Players",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Players",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ZoneName",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Players",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Players",
                newName: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Players",
                table: "Players",
                column: "Id");
        }
    }
}
