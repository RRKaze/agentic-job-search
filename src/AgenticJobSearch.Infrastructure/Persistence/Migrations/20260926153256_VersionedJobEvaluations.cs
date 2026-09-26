using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticJobSearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionedJobEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobEvaluations_JobId",
                table: "JobEvaluations");

            migrationBuilder.AddColumn<string>(
                name: "ProfileSnapshot",
                table: "JobEvaluations",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "ScoringVersion",
                table: "JobEvaluations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "legacy-v0");

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluations_JobId",
                table: "JobEvaluations",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobEvaluations_JobId",
                table: "JobEvaluations");

            migrationBuilder.DropColumn(
                name: "ProfileSnapshot",
                table: "JobEvaluations");

            migrationBuilder.DropColumn(
                name: "ScoringVersion",
                table: "JobEvaluations");

            migrationBuilder.Sql("""
                DELETE FROM "JobEvaluations" older
                USING "JobEvaluations" newer
                WHERE older."JobId" = newer."JobId"
                  AND (older."EvaluatedAt", older."Id") < (newer."EvaluatedAt", newer."Id");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_JobEvaluations_JobId",
                table: "JobEvaluations",
                column: "JobId",
                unique: true);
        }
    }
}
