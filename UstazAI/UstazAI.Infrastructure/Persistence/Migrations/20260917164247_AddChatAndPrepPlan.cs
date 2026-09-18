using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UstazAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatAndPrepPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Resources",
                table: "RoadmapTasks",
                type: "nvarchar(max)",
                nullable: false,
                // "" is not valid JSON — RoadmapTask.Resources is List<ResourceLink> via
                // HasJsonConversion<T>(), whose reader is JsonSerializer.Deserialize<T>(v) ??
                // new T(); the "?? new T()" only covers a *successful* deserialize that yields
                // null, not a parse failure, and Deserialize<List<T>>("") throws JsonException
                // ("The input does not contain any tokens"). "[]" is the actual empty-list JSON.
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "RoadmapTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "bit", nullable: false),
                    FallbackUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_StudentProfileId_CreatedAtUtc",
                table: "ChatMessages",
                columns: new[] { "StudentProfileId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Resources",
                table: "RoadmapTasks");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "RoadmapTasks");
        }
    }
}
