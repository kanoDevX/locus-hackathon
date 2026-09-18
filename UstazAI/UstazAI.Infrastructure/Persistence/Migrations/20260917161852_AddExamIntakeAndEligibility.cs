using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UstazAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamIntakeAndEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollegeBackground",
                table: "StudentProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationStage",
                table: "StudentProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingTrackPreference",
                table: "StudentProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                // "" is not a valid FundingTrackPreference enum member — StudentProfile.cs's own
                // C# default is Flexible, so any pre-existing row backfilled by this migration
                // must match it, or the very next EF Core read of that row throws (HasConversion
                // <string>() calls Enum.Parse on load, and Enum.Parse("") always fails).
                defaultValue: "Flexible");

            migrationBuilder.CreateTable(
                name: "AdmissionThresholds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    Track = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StateThreshold = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    UniversityInternalThreshold = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    HistoricalCutoffMin = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    HistoricalCutoffMax = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    HistoricalCutoffMedian = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    HistoricalCutoffSampleSize = table.Column<int>(type: "int", nullable: false),
                    Provenance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionThresholds_ProgramOfferings_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "ProgramOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileVersion = table.Column<int>(type: "int", nullable: false),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    Track = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MeetsStateThreshold = table.Column<bool>(type: "bit", nullable: false),
                    MeetsUniversityThreshold = table.Column<bool>(type: "bit", nullable: false),
                    GrantCompetitiveness = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidTrackEligible = table.Column<bool>(type: "bit", nullable: false),
                    IsDocumentOnlyVerdict = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provenance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityResults_ProgramOfferings_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "ProgramOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EligibilityResults_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileVersion = table.Column<int>(type: "int", nullable: false),
                    Track = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubjectBreakdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    DateTaken = table.Column<DateOnly>(type: "date", nullable: true),
                    Provenance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamRecords_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplementaryExamRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    DateTaken = table.Column<DateOnly>(type: "date", nullable: true),
                    Provenance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementaryExamRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplementaryExamRecords_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionThresholds_ProgramId_Track",
                table: "AdmissionThresholds",
                columns: new[] { "ProgramId", "Track" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityResults_ProgramId",
                table: "EligibilityResults",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityResults_StudentProfileId_ProfileVersion",
                table: "EligibilityResults",
                columns: new[] { "StudentProfileId", "ProfileVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamRecords_StudentProfileId_ProfileVersion",
                table: "ExamRecords",
                columns: new[] { "StudentProfileId", "ProfileVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplementaryExamRecords_StudentProfileId",
                table: "SupplementaryExamRecords",
                column: "StudentProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionThresholds");

            migrationBuilder.DropTable(
                name: "EligibilityResults");

            migrationBuilder.DropTable(
                name: "ExamRecords");

            migrationBuilder.DropTable(
                name: "SupplementaryExamRecords");

            migrationBuilder.DropColumn(
                name: "CollegeBackground",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "EducationStage",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "FundingTrackPreference",
                table: "StudentProfiles");
        }
    }
}
