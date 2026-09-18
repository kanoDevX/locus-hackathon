using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UstazAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoritePrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoritePrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoritePrograms_ProgramOfferings_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "ProgramOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FavoritePrograms_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoritePrograms_ProgramId",
                table: "FavoritePrograms",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoritePrograms_StudentProfileId_ProgramId",
                table: "FavoritePrograms",
                columns: new[] { "StudentProfileId", "ProgramId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoritePrograms");
        }
    }
}
