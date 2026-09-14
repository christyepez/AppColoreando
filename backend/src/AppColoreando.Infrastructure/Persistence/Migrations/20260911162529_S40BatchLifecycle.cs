using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppColoreando.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class S40BatchLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "ArtworkGenerationJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkGenerationJobs_BatchId",
                table: "ArtworkGenerationJobs",
                column: "BatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArtworkGenerationJobs_BatchId",
                table: "ArtworkGenerationJobs");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "ArtworkGenerationJobs");
        }
    }
}
