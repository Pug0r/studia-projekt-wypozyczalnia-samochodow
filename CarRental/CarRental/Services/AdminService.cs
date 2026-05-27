using CarRental.Data;
using CarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Services;

public sealed class AdminService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; 
    
    public AdminService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Car>> GetAllCarsAsync()
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        return await db.Cars.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<string?> AddCarAsync(string brand, string model, int year, decimal dailyRate, bool isAvailable, int mileage, string faults)
    {
        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model))
            return "Brand and Model are required.";
        if (year < 1900 || year > DateTime.Now.Year + 1)
            return "Please enter a valid year.";
        if (dailyRate <= 0)
            return "Daily rate must be greater than zero.";
        if (mileage < 0)
            return "Mileage cannot be negative.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        db.Cars.Add(new Car
        {
            Brand = brand.Trim(),
            Model = model.Trim(),
            Year = year,
            DailyRate = dailyRate,
            IsAvailable = isAvailable,
            Mileage = mileage,
            Faults = faults.Trim()
        });
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> UpdateCarAsync(int carId, string brand, string model, int year, decimal dailyRate, int mileage, string faults)
    {
        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model))
            return "Brand and Model are required.";
        if (year < 1900 || year > DateTime.Now.Year + 1)
            return "Please enter a valid year.";
        if (dailyRate <= 0)
            return "Daily rate must be greater than zero.";
        if (mileage < 0)
            return "Mileage cannot be negative.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        Car? car = await db.Cars.FindAsync(carId);
        if (car is null) return "Car not found.";

        car.Brand = brand.Trim();
        car.Model = model.Trim();
        car.Year = year;
        car.DailyRate = dailyRate;
        car.Mileage = mileage;
        car.Faults = faults.Trim();

        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> DeleteCarAsync(int carId)
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        Car? car = await db.Cars.FindAsync(carId);
        if (car is null) return "Car not found.";
        db.Cars.Remove(car);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
    }

    public async Task<string?> DeleteUserAsync(int userId, int requestingUserId)
    {
        if (userId == requestingUserId)
            return "You cannot delete your own account.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        User? user = await db.Users.FindAsync(userId);
        if (user is null) return "User not found.";
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> AddUserAsync(string username, string password, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return "Username must be at least 3 characters.";
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return "Password must be at least 6 characters.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Username == username))
            return "That username is already taken.";

        db.Users.Add(new User
        {
            Username = username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        });
        await db.SaveChangesAsync();
        return null;
    }
}
