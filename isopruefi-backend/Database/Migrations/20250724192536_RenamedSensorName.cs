using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class RenamedSensorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SensorType",
                table: "TopicSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SensorName",
                table: "TopicSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DefaultTopicPath",
                table: "TopicSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 1,
                column: "SensorName",
                value: "Sensor_One");

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 2,
                column: "SensorName",
                value: "Sensor-Two");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SensorType",
                table: "TopicSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SensorName",
                table: "TopicSettings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DefaultTopicPath",
                table: "TopicSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 1,
                column: "SensorName",
                value: "SENSOR-ONE");

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 2,
                column: "SensorName",
                value: "SENSOR-TWO");
        }
    }
}
