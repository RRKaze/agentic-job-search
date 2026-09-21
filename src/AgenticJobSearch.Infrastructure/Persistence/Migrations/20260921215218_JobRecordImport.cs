using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticJobSearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JobRecordImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "SourceUrl",
                table: "Jobs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "Jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FitRationale",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GapsNotes",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportedRecord",
                table: "Jobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalaryText",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRepository",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StatusDate",
                table: "Jobs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingStage",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "VerifiedDate",
                table: "Jobs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImportedRecord",
                table: "Applications",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextFollowUp",
                table: "Applications",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralContact",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeVersion",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRepository",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SubmittedDate",
                table: "Applications",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportCheckpoints",
                columns: table => new
                {
                    Source = table.Column<string>(type: "text", nullable: false),
                    Commit = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "text", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportCheckpoints", x => x.Source);
                });

            migrationBuilder.CreateTable(
                name: "TrackingChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Entity = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Previous = table.Column<string>(type: "text", nullable: true),
                    Current = table.Column<string>(type: "text", nullable: false),
                    Commit = table.Column<string>(type: "text", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_SourceRepository_ExternalId",
                table: "Jobs",
                columns: new[] { "SourceRepository", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_SourceRepository_ExternalId",
                table: "Applications",
                columns: new[] { "SourceRepository", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportCheckpoints");

            migrationBuilder.DropTable(
                name: "TrackingChanges");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_SourceRepository_ExternalId",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Applications_SourceRepository_ExternalId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FitRationale",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "GapsNotes",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ImportedRecord",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalaryText",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SourceRepository",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "StatusDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TrackingStage",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "VerifiedDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ImportedRecord",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "NextFollowUp",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ReferralContact",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ResumeVersion",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SourceRepository",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "SubmittedDate",
                table: "Applications");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Jobs",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "SourceUrl",
                table: "Jobs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Jobs",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "Jobs",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
        }
    }
}
