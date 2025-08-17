using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class CoordinateMappingLocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsed",
                table: "CoordinateMappings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CoordinateMappings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "CoordinateMappings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.InsertData(
                table: "CoordinateMappings",
                columns: new[] { "PostalCode", "LastUsed", "Latitude", "Location", "LockedUntil", "Longitude" },
                values: new object[] { 89518, null, 48.685200000000002, "Heidenheim an der Brenz", null, 10.1287 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CoordinateMappings",
                keyColumn: "PostalCode",
                keyValue: 89518);

            migrationBuilder.DropColumn(
                name: "Location",
                table: "CoordinateMappings");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "CoordinateMappings");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUsed",
                table: "CoordinateMappings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
