using CarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<User> Users => Set<User>();

    public DbSet<Rental> Rentals => Set<Rental>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>(entity =>
        {
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.DailyRate).HasPrecision(10, 2);
            entity.Property(e => e.Faults).HasMaxLength(200);
            entity.Property(e => e.ImageContentType).HasMaxLength(100);
            entity.Property(e => e.ImageFileName).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasOne(r => r.Car)
                  .WithMany()
                  .HasForeignKey(r => r.CarId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.TotalCost).HasPrecision(10, 2);
        });
    }
}
