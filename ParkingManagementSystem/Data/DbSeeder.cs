using ParkingManagementSystem.Models;
using ParkingManagementSystem.Services;

namespace ParkingManagementSystem.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext db, IPasswordHasher hasher)
    {
        db.Database.EnsureCreated();

        if (!db.Users.Any(u => u.Role == "Admin"))
        {
            var admin = new User
            {
                FullName = "System Administrator",
                Email = "admin@parking.local",
                PhoneNumber = "0000000000",
                VehiclePlateNumber = "ADMIN-001",
                PasswordHash = hasher.Hash("Admin@123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Users.Add(admin);
            db.SaveChanges();
        }
    }
}
