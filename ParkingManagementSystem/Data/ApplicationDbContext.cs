using Microsoft.EntityFrameworkCore;
using ParkingManagementSystem.Models;

namespace ParkingManagementSystem.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ParkingSlot> ParkingSlots => Set<ParkingSlot>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(u => u.VehiclePlateNumber).HasMaxLength(20).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).HasDefaultValue("User");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(t => t.Token).IsUnique();
            entity.Property(t => t.Token).HasMaxLength(128).IsRequired();
            entity.HasOne(t => t.User)
                  .WithMany(u => u.PasswordResetTokens)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParkingSlot>(entity =>
        {
            entity.Property(s => s.SlotNumber).HasMaxLength(20).IsRequired();
            entity.Property(s => s.SlotType).HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.HourlyRate).HasColumnType("decimal(10,2)");
            entity.Property(s => s.Description).HasMaxLength(500);

            // Unique slot number among non-deleted rows (allows reusing a number after soft-delete).
            entity.HasIndex(s => s.SlotNumber)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Fee).HasColumnType("decimal(10,2)");
            entity.Property(r => r.ExtensionFee).HasColumnType("decimal(10,2)");
            entity.Property(r => r.QrToken).HasMaxLength(256).IsRequired();
            entity.Property(r => r.PaymentReference).HasMaxLength(50);

            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Slot)
                  .WithMany()
                  .HasForeignKey(r => r.SlotId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Frequent lookups: by user (history) and by slot+time window (overlap check, scanner).
            entity.HasIndex(r => new { r.UserId, r.StartTime })
                  .HasDatabaseName("IX_Reservations_UserId_StartTime");

            entity.HasIndex(r => new { r.SlotId, r.StartTime, r.EndTime, r.Status })
                  .HasDatabaseName("IX_Reservations_SlotId_TimeRange_Status");

            // Unique QR token lookup for the scanner.
            entity.HasIndex(r => r.QrToken)
                  .IsUnique()
                  .HasDatabaseName("IX_Reservations_QrToken");
        });
    }
}
