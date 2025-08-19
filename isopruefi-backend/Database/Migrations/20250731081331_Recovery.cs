using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class Recovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasRecovery",
                table: "TopicSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 1,
                column: "HasRecovery",
                value: false);

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 2,
                column: "HasRecovery",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasRecovery",
                table: "TopicSettings");
        }
    }
}
