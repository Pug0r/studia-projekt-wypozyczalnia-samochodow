using CarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, bool enable)
    {
        if (!enable)
        {
            return;
        }

        if (await dbContext.Cars.AnyAsync())
        {
            return;
        }

        var cars = new List<Car>
        {
            new() { Make = "Toyota", Model = "Corolla", Year = 2021, DailyRate = 129.00m, IsAvailable = true },
            new() { Make = "Skoda", Model = "Octavia", Year = 2022, DailyRate = 149.00m, IsAvailable = true },
            new() { Make = "Volkswagen", Model = "Golf", Year = 2020, DailyRate = 119.00m, IsAvailable = false },
            new() { Make = "Kia", Model = "Sportage", Year = 2023, DailyRate = 189.00m, IsAvailable = true }
        };

        dbContext.Cars.AddRange(cars);
        await dbContext.SaveChangesAsync();
    }
}

