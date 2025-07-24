using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicSettings",
                columns: table => new
                {
                    TopicSettingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DefaultTopicPath = table.Column<string>(type: "text", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    SensorType = table.Column<string>(type: "text", nullable: false),
                    SensorName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicSettings", x => x.TopicSettingId);
                });

            migrationBuilder.InsertData(
                table: "TopicSettings",
                columns: new[] { "TopicSettingId", "DefaultTopicPath", "GroupId", "SensorName", "SensorType" },
                values: new object[,]
                {
                    { 1, "dhbw/ai/si2023", 2, "SENSOR-ONE", "temp" },
                    { 2, "dhbw/ai/si2023", 2, "SENSOR-TWO", "temp" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicSettings");
        }
    }
}
