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
            dbContext.Cars.AddRange(
                new Car { Make = "Toyota",     Model = "Corolla",  Year = 2021, DailyRate = 129.00m, IsAvailable = true  },
                new Car { Make = "Skoda",      Model = "Octavia",  Year = 2022, DailyRate = 149.00m, IsAvailable = true  },
                new Car { Make = "Volkswagen", Model = "Golf",     Year = 2020, DailyRate = 119.00m, IsAvailable = false },
                new Car { Make = "Kia",        Model = "Sportage", Year = 2023, DailyRate = 189.00m, IsAvailable = true  }
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
}
