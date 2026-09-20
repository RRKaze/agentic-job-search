using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticJobSearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetLevel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TargetRoleFamilies = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequiresSponsorship = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceText = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Company = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Seniority = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    WorkMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LifecycleState = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Statement = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateEvidence_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Eligibility = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FitScore = table.Column<int>(type: "integer", nullable: false),
                    ApplicationPriority = table.Column<int>(type: "integer", nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobEvaluations_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedback_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobEvaluationFactors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobEvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    ScoreImpact = table.Column<int>(type: "integer", nullable: false),
                    Rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobEvaluationFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobEvaluationFactors_JobEvaluations_JobEvaluationId",
                        column: x => x.JobEvaluationId,
                        principalTable: "JobEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobId",
                table: "Applications",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateEvidence_CandidateProfileId",
                table: "CandidateEvidence",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_ApplicationId",
                table: "Feedback",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluationFactors_JobEvaluationId",
                table: "JobEvaluationFactors",
                column: "JobEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluations_JobId",
                table: "JobEvaluations",
                column: "JobId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateEvidence");

            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "JobEvaluationFactors");

            migrationBuilder.DropTable(
                name: "CandidateProfiles");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "JobEvaluations");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
