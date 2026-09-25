using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WayPoint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusReviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusReviews_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusReviews_PassengerProfiles_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "PassengerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriverReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverReviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverReviews_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriverReviews_PassengerProfiles_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "PassengerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusReviews_BookingId",
                table: "BusReviews",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BusReviews_BusId_BookingId",
                table: "BusReviews",
                columns: new[] { "BusId", "BookingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusReviews_PassengerId",
                table: "BusReviews",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverReviews_BookingId",
                table: "DriverReviews",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverReviews_DriverId_BookingId",
                table: "DriverReviews",
                columns: new[] { "DriverId", "BookingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverReviews_PassengerId",
                table: "DriverReviews",
                column: "PassengerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusReviews");

            migrationBuilder.DropTable(
                name: "DriverReviews");
        }
    }
}
