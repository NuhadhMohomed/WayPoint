using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WayPoint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationGrouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReservationGroupId",
                table: "SeatHolds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReservationGroupId",
                table: "SeatAllocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReservationGroupId",
                table: "PaymentAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_ReservationGroupId",
                table: "SeatHolds",
                column: "ReservationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_ReservationGroupId",
                table: "SeatAllocations",
                column: "ReservationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_ReservationGroupId",
                table: "PaymentAttempts",
                column: "ReservationGroupId");

            // Safe legacy migration strategy:
            // Ensure legacy holds are isolated and never silently merged together
            migrationBuilder.Sql(@"
                UPDATE ""SeatHolds""
                SET ""ReservationGroupId"" = ""Id""
                WHERE ""ReservationGroupId"" = '00000000-0000-0000-0000-000000000000' OR ""ReservationGroupId"" IS NULL;

                UPDATE ""SeatAllocations""
                SET ""ReservationGroupId"" = ""HoldId""
                WHERE ""HoldId"" IS NOT NULL AND ""ReservationGroupId"" IS NULL;

                UPDATE ""PaymentAttempts""
                SET ""ReservationGroupId"" = ""HoldId""
                WHERE ""HoldId"" IS NOT NULL AND ""ReservationGroupId"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SeatHolds_ReservationGroupId",
                table: "SeatHolds");

            migrationBuilder.DropIndex(
                name: "IX_SeatAllocations_ReservationGroupId",
                table: "SeatAllocations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_ReservationGroupId",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "ReservationGroupId",
                table: "SeatHolds");

            migrationBuilder.DropColumn(
                name: "ReservationGroupId",
                table: "SeatAllocations");

            migrationBuilder.DropColumn(
                name: "ReservationGroupId",
                table: "PaymentAttempts");
        }
    }
}
