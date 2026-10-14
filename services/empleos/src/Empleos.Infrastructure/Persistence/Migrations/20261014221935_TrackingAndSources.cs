using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Empleos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackingAndSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                schema: "jobs",
                table: "JobOffers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "jobs",
                table: "JobOffers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ApplicationEvents",
                schema: "jobs",
                columns: table => new
                {
                    JobOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(280)", maxLength: 280, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationEvents", x => new { x.JobOfferId, x.Id });
                    table.ForeignKey(
                        name: "FK_ApplicationEvents_JobOffers_JobOfferId",
                        column: x => x.JobOfferId,
                        principalSchema: "jobs",
                        principalTable: "JobOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Senders = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Rule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sources_Name",
                schema: "jobs",
                table: "Sources",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationEvents",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "Sources",
                schema: "jobs");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                schema: "jobs",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "jobs",
                table: "JobOffers");
        }
    }
}
