using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Empleos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OfferDetailsAndImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "jobs",
                table: "JobOffers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "jobs",
                table: "JobOffers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                schema: "jobs",
                table: "JobOffers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Salary",
                schema: "jobs",
                table: "JobOffers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                schema: "jobs",
                table: "JobOffers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkMode",
                schema: "jobs",
                table: "JobOffers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_ExternalId",
                schema: "jobs",
                table: "JobOffers",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobOffers_ExternalId",
                schema: "jobs",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "jobs",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "jobs",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Location",
                schema: "jobs",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Salary",
                schema: "jobs",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Url",
                schema: "jobs",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "WorkMode",
                schema: "jobs",
                table: "JobOffers");
        }
    }
}
