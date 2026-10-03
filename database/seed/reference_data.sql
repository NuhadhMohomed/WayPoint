-- =============================================================================
-- WayPoint Transit System — Baseline Reference & Seed Data
-- =============================================================================

-- 1. Foundational Roles
INSERT INTO "Roles" ("Id", "RoleName", "Description", "CreatedAt", "UpdatedAt")
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Admin', 'System Administrator with full access', NOW(), NOW()),
    ('22222222-2222-2222-2222-222222222222', 'TransportManager', 'Transport Manager with operational approval authority', NOW(), NOW()),
    ('33333333-3333-3333-3333-333333333333', 'Operator', 'Bus Operator / Dispatcher', NOW(), NOW()),
    ('44444444-4444-4444-4444-444444444444', 'Passenger', 'Travel Passenger', NOW(), NOW())
ON CONFLICT ("RoleName") DO NOTHING;

-- 2. Transit Amenities
INSERT INTO "Amenities" ("Id", "Name", "IconCode", "CreatedAt", "UpdatedAt")
VALUES
    ('a1111111-1111-1111-1111-111111111111', 'Air Conditioning', 'snowflake', NOW(), NOW()),
    ('a2222222-2222-2222-2222-222222222222', 'High-Speed Wi-Fi', 'wifi', NOW(), NOW()),
    ('a3333333-3333-3333-3333-333333333333', 'USB Charging Ports', 'battery-charging', NOW(), NOW()),
    ('a4444444-4444-4444-4444-444444444444', 'Reclining Seats', 'armchair', NOW(), NOW())
ON CONFLICT DO NOTHING;

-- 3. Core Corridors & Express Routes
INSERT INTO "Routes" ("Id", "RouteCode", "Name", "OriginCity", "DestinationCity", "TotalDistanceKm", "EstimatedDurationMinutes", "IsScenicCorridor", "CreatedAt", "UpdatedAt")
VALUES
    ('b1111111-1111-1111-1111-111111111111', 'EX-01', 'Southern Highway Coastal Express', 'Colombo Fort', 'Galle Central', 119.50, 105, TRUE, NOW(), NOW()),
    ('b2222222-2222-2222-2222-222222222222', 'EX-02', 'Central Highlands Scenic Corridor', 'Colombo Fort', 'Kandy Goods Shed', 115.20, 195, TRUE, NOW(), NOW()),
    ('b3333333-3333-3333-3333-333333333333', 'EX-03', 'Ella Tourist Vista Express', 'Colombo Fort', 'Ella Town', 205.80, 310, TRUE, NOW(), NOW())
ON CONFLICT ("RouteCode") DO NOTHING;

-- 4. Route Intermediate Stops
INSERT INTO "RouteStops" ("Id", "RouteId", "StopName", "SequenceOrder", "DistanceFromOriginKm", "EstimatedMinutesFromOrigin", "CreatedAt", "UpdatedAt")
VALUES
    -- EX-01 Stops
    ('c1111111-1111-1111-1111-111111111111', 'b1111111-1111-1111-1111-111111111111', 'Colombo Bastian Mawatha', 1, 0.00, 0, NOW(), NOW()),
    ('c1111111-1111-1111-1111-111111111112', 'b1111111-1111-1111-1111-111111111111', 'Makumbura Multimodal Hub', 2, 21.00, 25, NOW(), NOW()),
    ('c1111111-1111-1111-1111-111111111113', 'b1111111-1111-1111-1111-111111111111', 'Galle Central Terminal', 3, 119.50, 105, NOW(), NOW()),
    -- EX-02 Stops
    ('c2222222-2222-2222-2222-222222222221', 'b2222222-2222-2222-2222-222222222222', 'Colombo Bastian Mawatha', 1, 0.00, 0, NOW(), NOW()),
    ('c2222222-2222-2222-2222-222222222222', 'b2222222-2222-2222-2222-222222222222', 'Kadawatha Interchange', 2, 16.00, 25, NOW(), NOW()),
    ('c2222222-2222-2222-2222-222222222223', 'b2222222-2222-2222-2222-222222222222', 'Peradeniya Junction', 3, 108.00, 180, NOW(), NOW()),
    ('c2222222-2222-2222-2222-222222222224', 'b2222222-2222-2222-2222-222222222222', 'Kandy Goods Shed Terminal', 4, 115.20, 195, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- 5. Default Seat Layout (Standard 2x2 Express)
INSERT INTO "SeatLayouts" ("Id", "Name", "TotalRows", "TotalColumns", "CreatedAt", "UpdatedAt")
VALUES
    ('d1111111-1111-1111-1111-111111111111', 'Standard 2x2 Express (40 Seats)', 10, 4, NOW(), NOW())
ON CONFLICT DO NOTHING;
