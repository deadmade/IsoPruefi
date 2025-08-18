using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHasRecoveryField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 1,
                column: "HasRecovery",
                value: true);

            migrationBuilder.UpdateData(
                table: "TopicSettings",
                keyColumn: "TopicSettingId",
                keyValue: 2,
                column: "HasRecovery",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
