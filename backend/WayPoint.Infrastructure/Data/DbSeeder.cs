using Microsoft.EntityFrameworkCore;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Domain.Entities.Fleet;
using WayPoint.Domain.Entities.Identity;
using WayPoint.Domain.Entities.Journey;
using WayPoint.Domain.Enums;

namespace WayPoint.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(WayPointDbContext context, IPasswordHasher passwordHasher)
    {
        // 1. Roles
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { RoleName = UserRoleType.Admin.ToString(), Description = "System Administrator with full access" },
                new() { RoleName = UserRoleType.TransportManager.ToString(), Description = "Transport Manager with operational approval authority" },
                new() { RoleName = UserRoleType.Operator.ToString(), Description = "Bus Operator / Dispatcher" },
                new() { RoleName = UserRoleType.Passenger.ToString(), Description = "Travel Passenger" }
            };
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        var adminRole = await context.Roles.FirstAsync(r => r.RoleName == UserRoleType.Admin.ToString());
        var managerRole = await context.Roles.FirstAsync(r => r.RoleName == UserRoleType.TransportManager.ToString());
        var operatorRole = await context.Roles.FirstAsync(r => r.RoleName == UserRoleType.Operator.ToString());
        var passengerRole = await context.Roles.FirstAsync(r => r.RoleName == UserRoleType.Passenger.ToString());

        // 2. Default Test Users (Password: Password123!)
        if (!await context.Users.AnyAsync())
        {
            var defaultPassword = passwordHasher.HashPassword("Password123!");

            var admin = new User
            {
                Email = "admin@waypoint.lk",
                FullName = "System Administrator",
                PasswordHash = defaultPassword,
                RoleId = adminRole.Id,
                PhoneNumber = "+94771234567"
            };

            var manager = new User
            {
                Email = "manager@waypoint.lk",
                FullName = "Transport Manager",
                PasswordHash = defaultPassword,
                RoleId = managerRole.Id,
                PhoneNumber = "+94772345678"
            };

            var opUser = new User
            {
                Email = "operator@waypoint.lk",
                FullName = "Transit Operator",
                PasswordHash = defaultPassword,
                RoleId = operatorRole.Id,
                PhoneNumber = "+94773456789",
                OperatorProfile = new OperatorProfile
                {
                    OperatorCode = "OP-SLTB-WEST",
                    CompanyName = "Sri Lanka Transit Board (Western)",
                    AssignedRegion = "Western Province"
                }
            };

            var passenger = new User
            {
                Email = "passenger@waypoint.lk",
                FullName = "Nimal Silva",
                PasswordHash = defaultPassword,
                RoleId = passengerRole.Id,
                PhoneNumber = "+94774567890",
                PassengerProfile = new PassengerProfile
                {
                    NicOrPassport = "200012345678",
                    EmergencyContact = "+94779998877"
                }
            };

            await context.Users.AddRangeAsync(admin, manager, opUser, passenger);
            await context.SaveChangesAsync();
        }

        // 3. Seat Layout & Seats (2x2 Luxury - 40 seats)
        SeatLayout layout;
        if (!await context.SeatLayouts.AnyAsync())
        {
            layout = new SeatLayout
            {
                Name = "Standard 2x2 Express (40 Seats)",
                TotalRows = 10,
                TotalColumns = 4
            };
            await context.SeatLayouts.AddAsync(layout);
            await context.SaveChangesAsync();

            var seats = new List<Seat>();
            var colLetters = new[] { "A", "B", "C", "D" };
            for (var r = 1; r <= 10; r++)
            {
                for (var c = 0; c < 4; c++)
                {
                    seats.Add(new Seat
                    {
                        SeatLayoutId = layout.Id,
                        SeatNumber = $"{r}{colLetters[c]}",
                        RowIndex = r,
                        ColumnIndex = c + 1,
                        SeatClass = (c == 0 || c == 3) ? SeatClass.Window : SeatClass.Aisle
                    });
                }
            }
            await context.Seats.AddRangeAsync(seats);
            await context.SaveChangesAsync();
        }
        else
        {
            layout = await context.SeatLayouts.FirstAsync();
        }

        // 4. Amenities
        if (!await context.Amenities.AnyAsync())
        {
            var amenities = new List<Amenity>
            {
                new() { Name = "Air Conditioning", IconCode = "snowflake" },
                new() { Name = "High-Speed Wi-Fi", IconCode = "wifi" },
                new() { Name = "USB Charging Ports", IconCode = "battery-charging" },
                new() { Name = "Reclining Seats", IconCode = "armchair" }
            };
            await context.Amenities.AddRangeAsync(amenities);
            await context.SaveChangesAsync();
        }

        // 5. Buses & Drivers
        Bus bus1, bus2;
        if (!await context.Buses.AnyAsync())
        {
            bus1 = new Bus
            {
                RegistrationNumber = "ND-5421",
                BusClass = BusClass.Luxury,
                TotalSeatCapacity = 40,
                SeatLayoutId = layout.Id,
                IsUnderMaintenance = false
            };

            bus2 = new Bus
            {
                RegistrationNumber = "NC-8890",
                BusClass = BusClass.SuperLuxury,
                TotalSeatCapacity = 40,
                SeatLayoutId = layout.Id,
                IsUnderMaintenance = false
            };

            await context.Buses.AddRangeAsync(bus1, bus2);
            await context.SaveChangesAsync();
        }
        else
        {
            bus1 = await context.Buses.FirstAsync();
            bus2 = await context.Buses.Skip(1).FirstOrDefaultAsync() ?? bus1;
        }

        Driver driver1, driver2;
        if (!await context.Drivers.AnyAsync())
        {
            driver1 = new Driver
            {
                FullName = "Sunimal Perera",
                LicenseNumber = "DL-98214-SP",
                PhoneNumber = "+94712345678",
                Status = "Active"
            };

            driver2 = new Driver
            {
                FullName = "Kamal Wickramasinghe",
                LicenseNumber = "DL-44129-KW",
                PhoneNumber = "+94713456789",
                Status = "Active"
            };

            await context.Drivers.AddRangeAsync(driver1, driver2);
            await context.SaveChangesAsync();
        }
        else
        {
            driver1 = await context.Drivers.FirstAsync();
            driver2 = await context.Drivers.Skip(1).FirstOrDefaultAsync() ?? driver1;
        }

        // 6. Routes & Stops (Sri Lankan Corridors)
        if (!await context.Routes.AnyAsync())
        {
            var routeKandy = new Route
            {
                RouteCode = "RT-01",
                Name = "Colombo - Kandy Intercity Express",
                OriginCity = "Colombo",
                DestinationCity = "Kandy",
                TotalDistanceKm = 115.0m,
                IsActive = true,
                Stops = new List<RouteStop>
                {
                    new() { StopName = "Colombo Fort", SequenceOrder = 1, ArrivalOffsetMinutes = 0, DistanceFromOriginKm = 0 },
                    new() { StopName = "Kadawatha Interchange", SequenceOrder = 2, ArrivalOffsetMinutes = 30, DistanceFromOriginKm = 18.5m },
                    new() { StopName = "Nittambuwa", SequenceOrder = 3, ArrivalOffsetMinutes = 60, DistanceFromOriginKm = 40.0m },
                    new() { StopName = "Kegalle", SequenceOrder = 4, ArrivalOffsetMinutes = 110, DistanceFromOriginKm = 78.0m },
                    new() { StopName = "Peradeniya", SequenceOrder = 5, ArrivalOffsetMinutes = 160, DistanceFromOriginKm = 110.0m },
                    new() { StopName = "Kandy Goodshed Terminal", SequenceOrder = 6, ArrivalOffsetMinutes = 180, DistanceFromOriginKm = 115.0m }
                },
                BoardingPoints = new List<BoardingPoint>
                {
                    new() { PointName = "Colombo Fort Central Bus Stand", Landmark = "Opposite Fort Railway Station", Latitude = 6.9344m, Longitude = 79.8519m },
                    new() { PointName = "Kadawatha Highway Entrance", Landmark = "Kadawatha Interchange Terminal", Latitude = 7.0016m, Longitude = 79.9534m }
                }
            };

            var routeElla = new Route
            {
                RouteCode = "EX-08",
                Name = "Colombo - Ella Highland Scenic Corridor",
                OriginCity = "Colombo",
                DestinationCity = "Ella",
                TotalDistanceKm = 205.0m,
                IsActive = true,
                Stops = new List<RouteStop>
                {
                    new() { StopName = "Makumbura Multimodal Center", SequenceOrder = 1, ArrivalOffsetMinutes = 0, DistanceFromOriginKm = 0 },
                    new() { StopName = "Ratnapura", SequenceOrder = 2, ArrivalOffsetMinutes = 90, DistanceFromOriginKm = 85.0m },
                    new() { StopName = "Pelmadulla", SequenceOrder = 3, ArrivalOffsetMinutes = 120, DistanceFromOriginKm = 104.0m },
                    new() { StopName = "Balangoda", SequenceOrder = 4, ArrivalOffsetMinutes = 160, DistanceFromOriginKm = 135.0m },
                    new() { StopName = "Haputale", SequenceOrder = 5, ArrivalOffsetMinutes = 240, DistanceFromOriginKm = 180.0m },
                    new() { StopName = "Bandarawela", SequenceOrder = 6, ArrivalOffsetMinutes = 270, DistanceFromOriginKm = 195.0m },
                    new() { StopName = "Ella City Station", SequenceOrder = 7, ArrivalOffsetMinutes = 300, DistanceFromOriginKm = 205.0m }
                },
                TouristDestinations = new List<TouristDestination>
                {
                    new() { AttractionName = "Nine Arch Bridge", Description = "Colonial viaduct bridge nestled amidst tea estates in Demodara/Ella" },
                    new() { AttractionName = "Little Adam's Peak", Description = "Panoramic highland hiking trail offering 360-degree views" }
                }
            };

            var routeGalle = new Route
            {
                RouteCode = "EX-02",
                Name = "Colombo - Galle Southern Expressway Direct",
                OriginCity = "Colombo",
                DestinationCity = "Galle",
                TotalDistanceKm = 120.0m,
                IsActive = true,
                Stops = new List<RouteStop>
                {
                    new() { StopName = "Makumbura Multimodal Center", SequenceOrder = 1, ArrivalOffsetMinutes = 0, DistanceFromOriginKm = 0 },
                    new() { StopName = "Kurundugahahetekma Interchange", SequenceOrder = 2, ArrivalOffsetMinutes = 45, DistanceFromOriginKm = 75.0m },
                    new() { StopName = "Pinnaduwa Interchange (Galle)", SequenceOrder = 3, ArrivalOffsetMinutes = 75, DistanceFromOriginKm = 120.0m }
                }
            };

            await context.Routes.AddRangeAsync(routeKandy, routeElla, routeGalle);
            await context.SaveChangesAsync();

            // 7. Scheduled Services
            var now = DateTime.UtcNow.Date.AddDays(1);

            var srvKandy = new Service
            {
                ServiceCode = "SRV-COL-KDY-0700",
                RouteId = routeKandy.Id,
                BusId = bus1.Id,
                DriverId = driver1.Id,
                DepartureTime = now.AddHours(7),
                ArrivalTime = now.AddHours(10),
                BaseFare = 1450.0m,
                Status = ServiceStatus.Scheduled
            };

            var srvElla = new Service
            {
                ServiceCode = "SRV-COL-ELLA-0800",
                RouteId = routeElla.Id,
                BusId = bus2.Id,
                DriverId = driver2.Id,
                DepartureTime = now.AddHours(8),
                ArrivalTime = now.AddHours(13),
                BaseFare = 2850.0m,
                Status = ServiceStatus.Scheduled
            };

            var srvGalle = new Service
            {
                ServiceCode = "SRV-COL-GAL-0930",
                RouteId = routeGalle.Id,
                BusId = bus1.Id,
                DriverId = driver1.Id,
                DepartureTime = now.AddHours(9).AddMinutes(30),
                ArrivalTime = now.AddHours(10).AddMinutes(45),
                BaseFare = 1150.0m,
                Status = ServiceStatus.Scheduled
            };

            await context.Services.AddRangeAsync(srvKandy, srvElla, srvGalle);
            await context.SaveChangesAsync();

            // 8. Reviews
            if (!await context.BusReviews.AnyAsync())
            {
                // We need a dummy booking to attach reviews. Since we don't have bookings in this layer, we can skip seeding reviews here, 
                // OR we just create a dummy passenger and booking if needed. But it's better to just leave it for the Booking seeder or let users add it.
                // Wait, it's fine. I will just leave it empty if we don't have bookings seeded here yet.
            }
        }
    }
}
