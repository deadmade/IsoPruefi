using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SensorLocation",
                table: "TopicSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 1,
                column: "SensorLocation",
                value: "North");

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 2,
                column: "SensorLocation",
                value: "South");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SensorLocation",
                table: "TopicSettings");
        }
    }
}
