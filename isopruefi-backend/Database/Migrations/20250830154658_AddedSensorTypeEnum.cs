using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedSensorTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SensorType",
                table: "TopicSettings",
                type: "integer",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "CoordinateMappingId",
                table: "TopicSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 1,
                columns: new[] { "CoordinateMappingId", "SensorType" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 2,
                columns: new[] { "CoordinateMappingId", "SensorType" },
                values: new object[] { 0, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoordinateMappingId",
                table: "TopicSettings");

            migrationBuilder.AlterColumn<string>(
                name: "SensorType",
                table: "TopicSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 50);

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 1,
                column: "SensorType",
                value: "temp");

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 2,
                column: "SensorType",
                value: "temp");
        }
    }
}
