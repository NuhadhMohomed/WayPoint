using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WayPoint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatAllocationConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeatAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    PassengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AllocationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatAllocations_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatAllocations_PassengerProfiles_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "PassengerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeatAllocations_SeatHolds_HoldId",
                        column: x => x.HoldId,
                        principalTable: "SeatHolds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatAllocations_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeatAllocations_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_BookingId",
                table: "SeatAllocations",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_HoldId",
                table: "SeatAllocations",
                column: "HoldId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_PassengerId",
                table: "SeatAllocations",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_SeatId",
                table: "SeatAllocations",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_ServiceId_SeatId",
                table: "SeatAllocations",
                columns: new[] { "ServiceId", "SeatId" },
                unique: true);

            // Safe Backfill: Populate active seat holds
            migrationBuilder.Sql(@"
                INSERT INTO ""SeatAllocations"" (""Id"", ""ServiceId"", ""SeatId"", ""HoldId"", ""BookingId"", ""PassengerId"", ""AllocatedAt"", ""ExpiresAt"", ""AllocationType"", ""CreatedAt"", ""UpdatedAt"")
                SELECT 
                    gen_random_uuid(),
                    sh.""ServiceId"",
                    sh.""SeatId"",
                    sh.""Id"",
                    NULL,
                    sh.""PassengerId"",
                    sh.""HeldAt"",
                    sh.""HeldUntil"",
                    'Hold',
                    NOW(),
                    NOW()
                FROM ""SeatHolds"" sh
                WHERE sh.""Status"" = 'Held' AND sh.""HeldUntil"" > NOW()
                ON CONFLICT (""ServiceId"", ""SeatId"") DO NOTHING;
            ");

            // Safe Backfill: Populate confirmed bookings
            migrationBuilder.Sql(@"
                INSERT INTO ""SeatAllocations"" (""Id"", ""ServiceId"", ""SeatId"", ""HoldId"", ""BookingId"", ""PassengerId"", ""AllocatedAt"", ""ExpiresAt"", ""AllocationType"", ""CreatedAt"", ""UpdatedAt"")
                SELECT 
                    gen_random_uuid(),
                    b.""ServiceId"",
                    s.""Id"",
                    NULL,
                    b.""Id"",
                    b.""PassengerId"",
                    b.""CreatedAt"",
                    '9999-12-31 23:59:59+00',
                    'Booking',
                    NOW(),
                    NOW()
                FROM ""Bookings"" b
                JOIN ""Services"" srv ON b.""ServiceId"" = srv.""Id""
                JOIN ""Buses"" bus ON srv.""BusId"" = bus.""Id""
                JOIN ""Seats"" s ON s.""SeatLayoutId"" = bus.""SeatLayoutId""
                WHERE b.""Status"" = 'Confirmed'
                  AND s.""SeatNumber"" = ANY(string_to_array(b.""SeatNumbers"", ','))
                ON CONFLICT (""ServiceId"", ""SeatId"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeatAllocations");
        }
    }
}
