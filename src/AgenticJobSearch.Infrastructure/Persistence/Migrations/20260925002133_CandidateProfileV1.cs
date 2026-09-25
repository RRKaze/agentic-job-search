using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticJobSearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CandidateProfileV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Degree",
                table: "CandidateProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmploymentTypePreference",
                table: "CandidateProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FieldOfStudy",
                table: "CandidateProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GraduationYear",
                table: "CandidateProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Headline",
                table: "CandidateProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Institution",
                table: "CandidateProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "CandidateProfiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioUrl",
                table: "CandidateProfiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocations",
                table: "CandidateProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalSummary",
                table: "CandidateProfiles",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecentEmployer",
                table: "CandidateProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecentJobTitle",
                table: "CandidateProfiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResumeText",
                table: "CandidateProfiles",
                type: "character varying(30000)",
                maxLength: 30000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "CandidateProfiles",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetIndustries",
                table: "CandidateProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkAuthorization",
                table: "CandidateProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkModePreference",
                table: "CandidateProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "YearsExperience",
                table: "CandidateProfiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Degree",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "EmploymentTypePreference",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "FieldOfStudy",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "GraduationYear",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Headline",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Institution",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "LinkedInUrl",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PortfolioUrl",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredLocations",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ProfessionalSummary",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "RecentEmployer",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "RecentJobTitle",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeText",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "TargetIndustries",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "WorkAuthorization",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "WorkModePreference",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "YearsExperience",
                table: "CandidateProfiles");
        }
    }
}
