using Microsoft.EntityFrameworkCore;
using ParkingManagementSystem.Models;
using ParkingManagementSystem.Models.Enums;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext db, IPasswordHasher hasher)
    {
        db.Database.EnsureCreated();

        // For dev databases created during earlier sprints, bring the schema up to date
        // without forcing the user to wipe their database. Each helper is idempotent.
        EnsureParkingSlotsTable(db);
        EnsureReservationsTable(db);

        SeedAdmin(db, hasher);
        SeedSampleSlots(db);
    }

    private static void SeedAdmin(ApplicationDbContext db, IPasswordHasher hasher)
    {
        if (db.Users.Any(u => u.Role == "Admin")) return;

        db.Users.Add(new User
        {
            FullName = "System Administrator",
            Email = "admin@parking.local",
            PhoneNumber = "0000000000",
            VehiclePlateNumber = "ADMIN-001",
            PasswordHash = hasher.Hash("Admin@123"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
        db.SaveChanges();
    }

    private static void SeedSampleSlots(ApplicationDbContext db)
    {
        if (db.ParkingSlots.Any()) return;

        // Sample slots arranged around a central point so the map shows a realistic cluster.
        const double centerLat = 40.7589;
        const double centerLng = -73.9851;

        var samples = new[]
        {
            ("A-01", SlotType.Standard, SlotStatus.Available,   5.00m, 0.00010, 0.00015),
            ("A-02", SlotType.Standard, SlotStatus.Occupied,    5.00m, 0.00010, 0.00030),
            ("A-03", SlotType.Standard, SlotStatus.Available,   5.00m, 0.00010, 0.00045),
            ("A-04", SlotType.Standard, SlotStatus.Maintenance, 5.00m, 0.00010, 0.00060),
            ("B-01", SlotType.VIP,      SlotStatus.Available,  12.00m,-0.00010, 0.00015),
            ("B-02", SlotType.VIP,      SlotStatus.Occupied,   12.00m,-0.00010, 0.00030),
            ("B-03", SlotType.VIP,      SlotStatus.Available,  12.00m,-0.00010, 0.00045),
            ("C-01", SlotType.EV,       SlotStatus.Available,   8.50m,-0.00025, 0.00015),
            ("C-02", SlotType.EV,       SlotStatus.Available,   8.50m,-0.00025, 0.00030),
            ("C-03", SlotType.EV,       SlotStatus.Maintenance, 8.50m,-0.00025, 0.00045)
        };

        foreach (var (number, type, status, rate, dLat, dLng) in samples)
        {
            db.ParkingSlots.Add(new ParkingSlot
            {
                SlotNumber = number,
                SlotType = type,
                Status = status,
                HourlyRate = rate,
                Latitude = centerLat + dLat,
                Longitude = centerLng + dLng,
                Description = type switch
                {
                    SlotType.VIP => "Premium covered slot with extra clearance.",
                    SlotType.EV  => "Includes Type-2 EV charging connector.",
                    _            => null
                },
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        db.SaveChanges();
    }

    private static void EnsureReservationsTable(ApplicationDbContext db)
    {
        const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Reservations')
BEGIN
    CREATE TABLE [Reservations] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [SlotId] INT NOT NULL,
        [StartTime] DATETIME2 NOT NULL,
        [EndTime] DATETIME2 NOT NULL,
        [Status] NVARCHAR(20) NOT NULL,
        [Fee] DECIMAL(10,2) NOT NULL DEFAULT 0,
        [ExtensionFee] DECIMAL(10,2) NOT NULL DEFAULT 0,
        [ExtensionCount] INT NOT NULL DEFAULT 0,
        [QrToken] NVARCHAR(256) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [EntryTime] DATETIME2 NULL,
        [ExitTime] DATETIME2 NULL,
        [CancelledAt] DATETIME2 NULL,
        [ActualDuration] TIME NULL,
        CONSTRAINT [FK_Reservations_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
        CONSTRAINT [FK_Reservations_ParkingSlots] FOREIGN KEY ([SlotId]) REFERENCES [ParkingSlots]([Id])
    );

    CREATE INDEX [IX_Reservations_UserId_StartTime]
        ON [Reservations]([UserId], [StartTime]);

    CREATE INDEX [IX_Reservations_SlotId_TimeRange_Status]
        ON [Reservations]([SlotId], [StartTime], [EndTime], [Status]);

    CREATE UNIQUE INDEX [IX_Reservations_QrToken]
        ON [Reservations]([QrToken]);
END";
        db.Database.ExecuteSqlRaw(sql);
    }

    private static void EnsureParkingSlotsTable(ApplicationDbContext db)
    {
        const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ParkingSlots')
BEGIN
    CREATE TABLE [ParkingSlots] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SlotNumber] NVARCHAR(20) NOT NULL,
        [SlotType] NVARCHAR(20) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL,
        [HourlyRate] DECIMAL(10,2) NOT NULL,
        [Latitude] FLOAT NOT NULL,
        [Longitude] FLOAT NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [DeletedAt] DATETIME2 NULL
    );
    CREATE UNIQUE INDEX [IX_ParkingSlots_SlotNumber]
        ON [ParkingSlots]([SlotNumber])
        WHERE [IsDeleted] = 0;
END";
        db.Database.ExecuteSqlRaw(sql);
    }
}
