using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
