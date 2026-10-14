using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Empleos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jobs");

            migrationBuilder.CreateTable(
                name: "JobOffers",
                schema: "jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Portal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArrivalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StatusDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOffers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_ArrivalDate",
                schema: "jobs",
                table: "JobOffers",
                column: "ArrivalDate");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Status",
                schema: "jobs",
                table: "JobOffers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobOffers",
                schema: "jobs");
        }
    }
}
