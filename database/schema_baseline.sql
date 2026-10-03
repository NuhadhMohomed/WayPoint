CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "AiWorkflows" (
        "Id" uuid NOT NULL,
        "Objective" text NOT NULL,
        "Status" character varying(30) NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AiWorkflows" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Amenities" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "IconCode" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Amenities" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "AuditLogs" (
        "Id" uuid NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "ActorId" character varying(100) NOT NULL,
        "ActionType" character varying(50) NOT NULL,
        "EntityName" character varying(50) NOT NULL,
        "EntityId" character varying(100) NOT NULL,
        "BeforeStateJson" jsonb,
        "AfterStateJson" jsonb,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Drivers" (
        "Id" uuid NOT NULL,
        "FullName" character varying(100) NOT NULL,
        "LicenseNumber" character varying(50) NOT NULL,
        "PhoneNumber" character varying(20) NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Drivers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Roles" (
        "Id" uuid NOT NULL,
        "RoleName" character varying(50) NOT NULL,
        "Description" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Routes" (
        "Id" uuid NOT NULL,
        "RouteCode" character varying(20) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "OriginCity" character varying(50) NOT NULL,
        "DestinationCity" character varying(50) NOT NULL,
        "TotalDistanceKm" numeric(8,2) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Routes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "SeatLayouts" (
        "Id" uuid NOT NULL,
        "Name" character varying(50) NOT NULL,
        "TotalRows" integer NOT NULL,
        "TotalColumns" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SeatLayouts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "AiWorkflowSteps" (
        "Id" uuid NOT NULL,
        "AiWorkflowId" uuid NOT NULL,
        "AgentName" text NOT NULL,
        "StepOrder" integer NOT NULL,
        "StepDescription" text NOT NULL,
        "ExecutedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AiWorkflowSteps" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AiWorkflowSteps_AiWorkflows_AiWorkflowId" FOREIGN KEY ("AiWorkflowId") REFERENCES "AiWorkflows" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Email" character varying(150) NOT NULL,
        "PasswordHash" text NOT NULL,
        "FullName" character varying(100) NOT NULL,
        "PhoneNumber" text,
        "RoleId" uuid NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "BoardingPoints" (
        "Id" uuid NOT NULL,
        "RouteId" uuid NOT NULL,
        "PointName" character varying(100) NOT NULL,
        "Landmark" text,
        "Latitude" numeric(10,8),
        "Longitude" numeric(11,8),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BoardingPoints" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BoardingPoints_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "RouteStops" (
        "Id" uuid NOT NULL,
        "RouteId" uuid NOT NULL,
        "StopName" character varying(100) NOT NULL,
        "SequenceOrder" integer NOT NULL,
        "ArrivalOffsetMinutes" integer NOT NULL,
        "DistanceFromOriginKm" numeric(8,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RouteStops" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RouteStops_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "TouristDestinations" (
        "Id" uuid NOT NULL,
        "RouteId" uuid NOT NULL,
        "AttractionName" character varying(100) NOT NULL,
        "Description" text,
        "ImageUrl" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TouristDestinations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TouristDestinations_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Buses" (
        "Id" uuid NOT NULL,
        "RegistrationNumber" character varying(30) NOT NULL,
        "BusClass" character varying(30) NOT NULL,
        "TotalSeatCapacity" integer NOT NULL,
        "SeatLayoutId" uuid NOT NULL,
        "IsUnderMaintenance" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Buses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Buses_SeatLayouts_SeatLayoutId" FOREIGN KEY ("SeatLayoutId") REFERENCES "SeatLayouts" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Seats" (
        "Id" uuid NOT NULL,
        "SeatLayoutId" uuid NOT NULL,
        "SeatNumber" character varying(10) NOT NULL,
        "RowIndex" integer NOT NULL,
        "ColumnIndex" integer NOT NULL,
        "SeatClass" character varying(20) NOT NULL,
        "RowVersion" bytea,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Seats" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Seats_SeatLayouts_SeatLayoutId" FOREIGN KEY ("SeatLayoutId") REFERENCES "SeatLayouts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "AiToolCalls" (
        "Id" uuid NOT NULL,
        "AiWorkflowStepId" uuid NOT NULL,
        "ToolName" text NOT NULL,
        "ArgumentsJson" jsonb NOT NULL,
        "ResultJson" jsonb NOT NULL,
        "DurationMs" integer NOT NULL,
        "ExecutedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AiToolCalls" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AiToolCalls_AiWorkflowSteps_AiWorkflowStepId" FOREIGN KEY ("AiWorkflowStepId") REFERENCES "AiWorkflowSteps" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "AiValidationResults" (
        "Id" uuid NOT NULL,
        "AiWorkflowStepId" uuid NOT NULL,
        "RuleName" text NOT NULL,
        "Passed" boolean NOT NULL,
        "ValidationDetails" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AiValidationResults" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AiValidationResults_AiWorkflowSteps_AiWorkflowStepId" FOREIGN KEY ("AiWorkflowStepId") REFERENCES "AiWorkflowSteps" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "OperatorProfiles" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "OperatorCode" text NOT NULL,
        "CompanyName" text,
        "AssignedRegion" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OperatorProfiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OperatorProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "PassengerProfiles" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "NicOrPassport" text,
        "EmergencyContact" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PassengerProfiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PassengerProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "MaintenanceRecords" (
        "Id" uuid NOT NULL,
        "BusId" uuid NOT NULL,
        "Description" text NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "Cost" numeric(10,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MaintenanceRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MaintenanceRecords_Buses_BusId" FOREIGN KEY ("BusId") REFERENCES "Buses" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Services" (
        "Id" uuid NOT NULL,
        "ServiceCode" character varying(30) NOT NULL,
        "RouteId" uuid NOT NULL,
        "BusId" uuid NOT NULL,
        "DriverId" uuid,
        "DepartureTime" timestamp with time zone NOT NULL,
        "ArrivalTime" timestamp with time zone NOT NULL,
        "BaseFare" numeric(10,2) NOT NULL,
        "Status" character varying(30) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Services" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Services_Buses_BusId" FOREIGN KEY ("BusId") REFERENCES "Buses" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Services_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Services_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "JourneySearches" (
        "Id" uuid NOT NULL,
        "PassengerId" uuid,
        "OriginCity" text NOT NULL,
        "DestinationCity" text NOT NULL,
        "TravelDate" timestamp with time zone NOT NULL,
        "PassengerCount" integer NOT NULL,
        "SearchedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_JourneySearches" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_JourneySearches_PassengerProfiles_PassengerId" FOREIGN KEY ("PassengerId") REFERENCES "PassengerProfiles" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Bookings" (
        "Id" uuid NOT NULL,
        "BookingReference" character varying(20) NOT NULL,
        "PassengerId" uuid NOT NULL,
        "ServiceId" uuid NOT NULL,
        "SeatNumbers" text NOT NULL,
        "TotalFareAmount" numeric(10,2) NOT NULL,
        "Status" character varying(30) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Bookings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Bookings_PassengerProfiles_PassengerId" FOREIGN KEY ("PassengerId") REFERENCES "PassengerProfiles" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Bookings_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "DisruptionCases" (
        "Id" uuid NOT NULL,
        "DisruptedServiceId" uuid NOT NULL,
        "Reason" text NOT NULL,
        "Severity" character varying(20) NOT NULL,
        "AffectedPassengerCount" integer NOT NULL,
        "Status" character varying(30) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_DisruptionCases" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DisruptionCases_Services_DisruptedServiceId" FOREIGN KEY ("DisruptedServiceId") REFERENCES "Services" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "DriverAssignments" (
        "Id" uuid NOT NULL,
        "DriverId" uuid NOT NULL,
        "ServiceId" uuid NOT NULL,
        "AssignedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_DriverAssignments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DriverAssignments_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_DriverAssignments_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "FareRules" (
        "Id" uuid NOT NULL,
        "ServiceId" uuid NOT NULL,
        "BusClass" character varying(30) NOT NULL,
        "RatePerKm" numeric(8,2) NOT NULL,
        "ClassMultiplier" numeric(4,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FareRules" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FareRules_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "SeatHolds" (
        "Id" uuid NOT NULL,
        "ServiceId" uuid NOT NULL,
        "SeatId" uuid NOT NULL,
        "PassengerId" uuid NOT NULL,
        "HeldAt" timestamp with time zone NOT NULL,
        "HeldUntil" timestamp with time zone NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SeatHolds" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SeatHolds_PassengerProfiles_PassengerId" FOREIGN KEY ("PassengerId") REFERENCES "PassengerProfiles" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SeatHolds_Seats_SeatId" FOREIGN KEY ("SeatId") REFERENCES "Seats" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SeatHolds_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "ServiceAlerts" (
        "Id" uuid NOT NULL,
        "ServiceId" uuid NOT NULL,
        "Title" character varying(100) NOT NULL,
        "Message" text NOT NULL,
        "PostedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ServiceAlerts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ServiceAlerts_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "ServiceAmenities" (
        "ServiceId" uuid NOT NULL,
        "AmenityId" uuid NOT NULL,
        "Id" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ServiceAmenities" PRIMARY KEY ("ServiceId", "AmenityId"),
        CONSTRAINT "FK_ServiceAmenities_Amenities_AmenityId" FOREIGN KEY ("AmenityId") REFERENCES "Amenities" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ServiceAmenities_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "JourneyCandidates" (
        "Id" uuid NOT NULL,
        "JourneySearchId" uuid NOT NULL,
        "CandidateType" text NOT NULL,
        "TotalFare" numeric(10,2) NOT NULL,
        "TotalDurationMinutes" integer NOT NULL,
        "MatchScore" numeric(4,3) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_JourneyCandidates" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_JourneyCandidates_JourneySearches_JourneySearchId" FOREIGN KEY ("JourneySearchId") REFERENCES "JourneySearches" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "PaymentAttempts" (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "GatewayTransactionId" text NOT NULL,
        "Amount" numeric(10,2) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "ProcessedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PaymentAttempts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PaymentAttempts_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Refunds" (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "RefundAmount" numeric(10,2) NOT NULL,
        "Percentage" numeric(5,2) NOT NULL,
        "Reason" text NOT NULL,
        "ProcessedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Refunds" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Refunds_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "Tickets" (
        "Id" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "QrCodePayload" text NOT NULL,
        "Status" character varying(20) NOT NULL,
        "IsBoarded" boolean NOT NULL,
        "BoardedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Tickets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Tickets_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "RebookingProposals" (
        "Id" uuid NOT NULL,
        "DisruptionCaseId" uuid NOT NULL,
        "ReplacementServiceId" uuid NOT NULL,
        "ProposedByAgent" text NOT NULL,
        "Status" character varying(30) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RebookingProposals" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RebookingProposals_DisruptionCases_DisruptionCaseId" FOREIGN KEY ("DisruptionCaseId") REFERENCES "DisruptionCases" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_RebookingProposals_Services_ReplacementServiceId" FOREIGN KEY ("ReplacementServiceId") REFERENCES "Services" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE TABLE "ApprovalDecisions" (
        "Id" uuid NOT NULL,
        "RebookingProposalId" uuid NOT NULL,
        "ManagerId" uuid NOT NULL,
        "Decision" character varying(30) NOT NULL,
        "Comments" text,
        "DecidedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ApprovalDecisions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ApprovalDecisions_RebookingProposals_RebookingProposalId" FOREIGN KEY ("RebookingProposalId") REFERENCES "RebookingProposals" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ApprovalDecisions_Users_ManagerId" FOREIGN KEY ("ManagerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_AiToolCalls_AiWorkflowStepId" ON "AiToolCalls" ("AiWorkflowStepId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_AiValidationResults_AiWorkflowStepId" ON "AiValidationResults" ("AiWorkflowStepId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_AiWorkflowSteps_AiWorkflowId" ON "AiWorkflowSteps" ("AiWorkflowId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_ApprovalDecisions_ManagerId" ON "ApprovalDecisions" ("ManagerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_ApprovalDecisions_RebookingProposalId" ON "ApprovalDecisions" ("RebookingProposalId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_BoardingPoints_RouteId" ON "BoardingPoints" ("RouteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Bookings_BookingReference" ON "Bookings" ("BookingReference");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Bookings_PassengerId" ON "Bookings" ("PassengerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Bookings_ServiceId" ON "Bookings" ("ServiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Buses_RegistrationNumber" ON "Buses" ("RegistrationNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Buses_SeatLayoutId" ON "Buses" ("SeatLayoutId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_DisruptionCases_DisruptedServiceId" ON "DisruptionCases" ("DisruptedServiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_DriverAssignments_DriverId_ServiceId" ON "DriverAssignments" ("DriverId", "ServiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_DriverAssignments_ServiceId" ON "DriverAssignments" ("ServiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Drivers_LicenseNumber" ON "Drivers" ("LicenseNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_FareRules_ServiceId" ON "FareRules" ("ServiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_JourneyCandidates_JourneySearchId" ON "JourneyCandidates" ("JourneySearchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_JourneySearches_PassengerId" ON "JourneySearches" ("PassengerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_MaintenanceRecords_BusId" ON "MaintenanceRecords" ("BusId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_OperatorProfiles_OperatorCode" ON "OperatorProfiles" ("OperatorCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_OperatorProfiles_UserId" ON "OperatorProfiles" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_PassengerProfiles_UserId" ON "PassengerProfiles" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_PaymentAttempts_BookingId" ON "PaymentAttempts" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_RebookingProposals_DisruptionCaseId" ON "RebookingProposals" ("DisruptionCaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_RebookingProposals_ReplacementServiceId" ON "RebookingProposals" ("ReplacementServiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Refunds_BookingId" ON "Refunds" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Roles_RoleName" ON "Roles" ("RoleName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Routes_OriginCity_DestinationCity" ON "Routes" ("OriginCity", "DestinationCity");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Routes_RouteCode" ON "Routes" ("RouteCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_RouteStops_RouteId_SequenceOrder" ON "RouteStops" ("RouteId", "SequenceOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_SeatHolds_PassengerId" ON "SeatHolds" ("PassengerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_SeatHolds_SeatId" ON "SeatHolds" ("SeatId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_SeatHolds_ServiceId_SeatId_HeldUntil_Status" ON "SeatHolds" ("ServiceId", "SeatId", "HeldUntil", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Seats_SeatLayoutId_SeatNumber" ON "Seats" ("SeatLayoutId", "SeatNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_ServiceAlerts_ServiceId" ON "ServiceAlerts" ("ServiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_ServiceAmenities_AmenityId" ON "ServiceAmenities" ("AmenityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Services_BusId" ON "Services" ("BusId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Services_DriverId" ON "Services" ("DriverId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Services_RouteId_DepartureTime_Status" ON "Services" ("RouteId", "DepartureTime", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Services_ServiceCode" ON "Services" ("ServiceCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Tickets_BookingId" ON "Tickets" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_TouristDestinations_RouteId" ON "TouristDestinations" ("RouteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    CREATE INDEX "IX_Users_RoleId" ON "Users" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907093703_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260907093703_InitialCreate', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    CREATE TABLE "BusReviews" (
        "Id" uuid NOT NULL,
        "BusId" uuid NOT NULL,
        "PassengerId" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "Comment" character varying(1000),
        "IsAnonymous" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BusReviews" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BusReviews_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_BusReviews_Buses_BusId" FOREIGN KEY ("BusId") REFERENCES "Buses" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_BusReviews_PassengerProfiles_PassengerId" FOREIGN KEY ("PassengerId") REFERENCES "PassengerProfiles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    CREATE TABLE "DriverReviews" (
        "Id" uuid NOT NULL,
        "DriverId" uuid NOT NULL,
        "PassengerId" uuid NOT NULL,
        "BookingId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "Comment" character varying(1000),
        "IsAnonymous" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_DriverReviews" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DriverReviews_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DriverReviews_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_DriverReviews_PassengerProfiles_PassengerId" FOREIGN KEY ("PassengerId") REFERENCES "PassengerProfiles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    CREATE INDEX "IX_BusReviews_BookingId" ON "BusReviews" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    CREATE UNIQUE INDEX "IX_BusReviews_BusId_BookingId" ON "BusReviews" ("BusId", "BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    CREATE INDEX "IX_BusReviews_PassengerId" ON "BusReviews" ("PassengerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    CREATE INDEX "IX_DriverReviews_BookingId" ON "DriverReviews" ("BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    CREATE UNIQUE INDEX "IX_DriverReviews_DriverId_BookingId" ON "DriverReviews" ("DriverId", "BookingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    CREATE INDEX "IX_DriverReviews_PassengerId" ON "DriverReviews" ("PassengerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260911152247_AddReviewEntities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260911152247_AddReviewEntities', '9.0.1');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003094506_AddAuditLogHashAndNotifications') THEN
    ALTER TABLE "JourneyCandidates" ALTER COLUMN "CandidateType" TYPE character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003094506_AddAuditLogHashAndNotifications') THEN
    ALTER TABLE "Drivers" ALTER COLUMN "Status" TYPE character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003094506_AddAuditLogHashAndNotifications') THEN
    ALTER TABLE "AuditLogs" ADD "HashSha256" character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003094506_AddAuditLogHashAndNotifications') THEN
    CREATE TABLE "Notifications" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Title" character varying(150) NOT NULL,
        "Message" character varying(1000) NOT NULL,
        "Type" character varying(50) NOT NULL,
        "ReferenceEntityType" character varying(50),
        "ReferenceEntityId" uuid,
        "IsRead" boolean NOT NULL,
        "ReadAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003094506_AddAuditLogHashAndNotifications') THEN
    CREATE INDEX "IX_Notifications_UserId_CreatedAt" ON "Notifications" ("UserId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003094506_AddAuditLogHashAndNotifications') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261003094506_AddAuditLogHashAndNotifications', '9.0.1');
    END IF;
END $EF$;
COMMIT;

