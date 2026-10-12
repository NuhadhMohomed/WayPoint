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

            // 1. Create audit table for recording any pre-existing historical seat allocation conflicts
            migrationBuilder.CreateTable(
                name: "SeatAllocationConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConflictType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ConflictingEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConflictingReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolutionDetails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatAllocationConflicts", x => x.Id);
                });

            // 2. Audit: Detect pre-existing duplicate confirmed bookings (historical double bookings before Fix 03)
            migrationBuilder.Sql(@"
                INSERT INTO ""SeatAllocationConflicts"" (""Id"", ""ServiceId"", ""SeatId"", ""SeatNumber"", ""ConflictType"", ""ConflictingEntityId"", ""ConflictingReference"", ""ResolutionDetails"", ""DetectedAt"")
                WITH ExpandedBookings AS (
                    SELECT 
                        b.""Id"" as ""BookingId"",
                        b.""BookingReference"",
                        b.""ServiceId"",
                        s.""Id"" as ""SeatId"",
                        s.""SeatNumber"",
                        b.""CreatedAt"",
                        ROW_NUMBER() OVER (PARTITION BY b.""ServiceId"", s.""Id"" ORDER BY b.""CreatedAt"" ASC) as rn
                    FROM ""Bookings"" b
                    JOIN ""Services"" srv ON b.""ServiceId"" = srv.""Id""
                    JOIN ""Buses"" bus ON srv.""BusId"" = bus.""Id""
                    JOIN ""Seats"" s ON s.""SeatLayoutId"" = bus.""SeatLayoutId""
                    WHERE b.""Status"" = 'Confirmed'
                      AND s.""SeatNumber"" = ANY(string_to_array(b.""SeatNumbers"", ','))
                )
                SELECT 
                    gen_random_uuid(),
                    eb.""ServiceId"",
                    eb.""SeatId"",
                    eb.""SeatNumber"",
                    'HistoricalDuplicateConfirmedBooking',
                    eb.""BookingId"",
                    eb.""BookingReference"",
                    'Detected conflicting confirmed booking for same seat. Earliest booking retained in SeatAllocations; flagged for manual review.',
                    NOW()
                FROM ExpandedBookings eb
                WHERE eb.rn > 1;
            ");

            // 3. Backfill Confirmed Bookings FIRST: Earliest confirmed booking gets the seat allocation
            migrationBuilder.Sql(@"
                INSERT INTO ""SeatAllocations"" (""Id"", ""ServiceId"", ""SeatId"", ""HoldId"", ""BookingId"", ""PassengerId"", ""AllocatedAt"", ""ExpiresAt"", ""AllocationType"", ""CreatedAt"", ""UpdatedAt"")
                WITH RankedBookings AS (
                    SELECT 
                        b.""Id"" as ""BookingId"",
                        b.""ServiceId"",
                        s.""Id"" as ""SeatId"",
                        b.""PassengerId"",
                        b.""CreatedAt"",
                        ROW_NUMBER() OVER (PARTITION BY b.""ServiceId"", s.""Id"" ORDER BY b.""CreatedAt"" ASC) as rn
                    FROM ""Bookings"" b
                    JOIN ""Services"" srv ON b.""ServiceId"" = srv.""Id""
                    JOIN ""Buses"" bus ON srv.""BusId"" = bus.""Id""
                    JOIN ""Seats"" s ON s.""SeatLayoutId"" = bus.""SeatLayoutId""
                    WHERE b.""Status"" = 'Confirmed'
                      AND s.""SeatNumber"" = ANY(string_to_array(b.""SeatNumbers"", ','))
                )
                SELECT 
                    gen_random_uuid(),
                    rb.""ServiceId"",
                    rb.""SeatId"",
                    NULL,
                    rb.""BookingId"",
                    rb.""PassengerId"",
                    rb.""CreatedAt"",
                    '9999-12-31 23:59:59+00',
                    'Booking',
                    NOW(),
                    NOW()
                FROM RankedBookings rb
                WHERE rb.rn = 1
                ON CONFLICT (""ServiceId"", ""SeatId"") DO NOTHING;
            ");

            // 4. Audit: Detect any active holds that overlap with confirmed bookings
            migrationBuilder.Sql(@"
                INSERT INTO ""SeatAllocationConflicts"" (""Id"", ""ServiceId"", ""SeatId"", ""SeatNumber"", ""ConflictType"", ""ConflictingEntityId"", ""ConflictingReference"", ""ResolutionDetails"", ""DetectedAt"")
                SELECT 
                    gen_random_uuid(),
                    sh.""ServiceId"",
                    sh.""SeatId"",
                    s.""SeatNumber"",
                    'ActiveHoldOverlapsConfirmedBooking',
                    sh.""Id"",
                    sh.""Id""::text,
                    'Active seat hold conflicted with an already confirmed booking. Expired hold status to prevent invalid checkout.',
                    NOW()
                FROM ""SeatHolds"" sh
                JOIN ""Seats"" s ON sh.""SeatId"" = s.""Id""
                WHERE sh.""Status"" = 'Held' AND sh.""HeldUntil"" > NOW()
                  AND EXISTS (
                      SELECT 1 FROM ""SeatAllocations"" sa 
                      WHERE sa.""ServiceId"" = sh.""ServiceId"" AND sa.""SeatId"" = sh.""SeatId""
                  );
            ");

            // 5. Expire any active holds that overlapped with confirmed bookings to prevent checkout corruption
            migrationBuilder.Sql(@"
                UPDATE ""SeatHolds"" sh
                SET ""Status"" = 'Expired'
                WHERE sh.""Status"" = 'Held' AND sh.""HeldUntil"" > NOW()
                  AND EXISTS (
                      SELECT 1 FROM ""SeatAllocations"" sa 
                      WHERE sa.""ServiceId"" = sh.""ServiceId"" AND sa.""SeatId"" = sh.""SeatId""
                  );
            ");

            // 6. Backfill remaining valid active holds
            migrationBuilder.Sql(@"
                INSERT INTO ""SeatAllocations"" (""Id"", ""ServiceId"", ""SeatId"", ""HoldId"", ""BookingId"", ""PassengerId"", ""AllocatedAt"", ""ExpiresAt"", ""AllocationType"", ""CreatedAt"", ""UpdatedAt"")
                WITH RankedHolds AS (
                    SELECT 
                        sh.""Id"" as ""HoldId"",
                        sh.""ServiceId"",
                        sh.""SeatId"",
                        sh.""PassengerId"",
                        sh.""HeldAt"",
                        sh.""HeldUntil"",
                        ROW_NUMBER() OVER (PARTITION BY sh.""ServiceId"", sh.""SeatId"" ORDER BY sh.""HeldAt"" ASC) as rn
                    FROM ""SeatHolds"" sh
                    WHERE sh.""Status"" = 'Held' AND sh.""HeldUntil"" > NOW()
                )
                SELECT 
                    gen_random_uuid(),
                    rh.""ServiceId"",
                    rh.""SeatId"",
                    rh.""HoldId"",
                    NULL,
                    rh.""PassengerId"",
                    rh.""HeldAt"",
                    rh.""HeldUntil"",
                    'Hold',
                    NOW(),
                    NOW()
                FROM RankedHolds rh
                WHERE rh.rn = 1
                ON CONFLICT (""ServiceId"", ""SeatId"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeatAllocationConflicts");

            migrationBuilder.DropTable(
                name: "SeatAllocations");
        }
    }
}
