using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppColoreando.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContentGenerationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtworkGenerationIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtworkGenerationJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkGenerationIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArtworkGenerationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    StylePresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessorJobId = table.Column<string>(type: "text", nullable: true),
                    ResultManifestPath = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkGenerationJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StylePresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    TargetRegionCount = table.Column<int>(type: "integer", nullable: false),
                    MinRegionArea = table.Column<int>(type: "integer", nullable: false),
                    MaxColors = table.Column<int>(type: "integer", nullable: false),
                    SimplificationTolerance = table.Column<double>(type: "double precision", nullable: false),
                    EdgeSensitivity = table.Column<double>(type: "double precision", nullable: false),
                    CurveSmoothness = table.Column<double>(type: "double precision", nullable: false),
                    SaturationBoost = table.Column<double>(type: "double precision", nullable: false),
                    ContrastBoost = table.Column<double>(type: "double precision", nullable: false),
                    SemanticMergeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StylePresets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkGenerationIssues_ArtworkGenerationJobId",
                table: "ArtworkGenerationIssues",
                column: "ArtworkGenerationJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkGenerationJobs_Status_CreatedAtUtc",
                table: "ArtworkGenerationJobs",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceAssets_Sha256",
                table: "SourceAssets",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_StylePresets_Code",
                table: "StylePresets",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtworkGenerationIssues");

            migrationBuilder.DropTable(
                name: "ArtworkGenerationJobs");

            migrationBuilder.DropTable(
                name: "SourceAssets");

            migrationBuilder.DropTable(
                name: "StylePresets");
        }
    }
}
