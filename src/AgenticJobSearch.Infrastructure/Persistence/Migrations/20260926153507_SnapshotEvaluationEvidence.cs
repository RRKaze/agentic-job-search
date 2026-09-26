using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticJobSearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotEvaluationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobEvaluationEvidenceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobEvaluationFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Statement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobEvaluationEvidenceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobEvaluationEvidenceSnapshots_JobEvaluationFactors_JobEval~",
                        column: x => x.JobEvaluationFactorId,
                        principalTable: "JobEvaluationFactors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluationEvidenceSnapshots_JobEvaluationFactorId",
                table: "JobEvaluationEvidenceSnapshots",
                column: "JobEvaluationFactorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobEvaluationEvidenceSnapshots");
        }
    }
}
