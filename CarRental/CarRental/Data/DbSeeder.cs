using CarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, bool enable)
    {
        if (!enable)
            return;

        if (!await dbContext.Cars.AnyAsync())
        {
            var (toyotaData, toyotaType, toyotaName) = TryLoadImage("toyota_corolla.jpg");
            var (skodaData, skodaType, skodaName) = TryLoadImage("skoda_octavia.jpg");
            var (vwData, vwType, vwName) = TryLoadImage("volkswagen_golf.jpg");
            var (kiaData, kiaType, kiaName) = TryLoadImage("kia_sportage.jpg");

            dbContext.Cars.AddRange(
                new Car
                {
                    Brand = "Toyota",
                    Model = "Corolla",
                    Year = 2021,
                    DailyRate = 129.00m,
                    IsAvailable = true,
                    Mileage = 45000,
                    Faults = "",
                    ImageData = toyotaData,
                    ImageContentType = toyotaType,
                    ImageFileName = toyotaName
                },
                new Car
                {
                    Brand = "Skoda",
                    Model = "Octavia",
                    Year = 2022,
                    DailyRate = 149.00m,
                    IsAvailable = true,
                    Mileage = 22000,
                    Faults = "",
                    ImageData = skodaData,
                    ImageContentType = skodaType,
                    ImageFileName = skodaName
                },
                new Car
                {
                    Brand = "Volkswagen",
                    Model = "Golf",
                    Year = 2020,
                    DailyRate = 119.00m,
                    IsAvailable = false,
                    Mileage = 78000,
                    Faults = "Minor scratch on bumper",
                    ImageData = vwData,
                    ImageContentType = vwType,
                    ImageFileName = vwName
                },
                new Car
                {
                    Brand = "Kia",
                    Model = "Sportage",
                    Year = 2023,
                    DailyRate = 189.00m,
                    IsAvailable = true,
                    Mileage = 8500,
                    Faults = "",
                    ImageData = kiaData,
                    ImageContentType = kiaType,
                    ImageFileName = kiaName
                }
            );
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Users.AnyAsync())
        {
            dbContext.Users.AddRange(
                new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), Role = UserRole.Admin },
                new User { Username = "customer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"), Role = UserRole.Customer }
            );
            await dbContext.SaveChangesAsync();
        }
    }

    private static (byte[]? data, string? contentType, string? fileName) TryLoadImage(string fileName)
    {
        string imagePath = Path.Combine(AppContext.BaseDirectory, "Data", "images", fileName);
        if (!File.Exists(imagePath))
            return (null, null, null);

        string extension = Path.GetExtension(imagePath).ToLowerInvariant();
        string contentType = extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        return (File.ReadAllBytes(imagePath), contentType, Path.GetFileName(imagePath));
    }
}
