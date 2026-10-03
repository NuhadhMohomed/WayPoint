using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WayPoint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLockoutAndJourneyLegs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JourneyLegs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JourneyCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegOrder = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardingStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlightingStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    LegFare = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneyLegs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JourneyLegs_JourneyCandidates_JourneyCandidateId",
                        column: x => x.JourneyCandidateId,
                        principalTable: "JourneyCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JourneyLegs_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JourneyLegs_JourneyCandidateId",
                table: "JourneyLegs",
                column: "JourneyCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_JourneyLegs_ServiceId",
                table: "JourneyLegs",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JourneyLegs");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Users");
        }
    }
}
