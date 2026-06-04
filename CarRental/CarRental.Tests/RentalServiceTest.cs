using CarRental.Data;
using CarRental.Models;
using CarRental.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.Tests;

public sealed class RentalServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<AppDbContext> options;
    private readonly TestDbContextFactory dbFactory;
    private readonly AuthService authService;
    private readonly RentalService service;

    public RentalServiceTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        using var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();

        dbFactory = new TestDbContextFactory(options);
        authService = new AuthService(dbFactory);

        service = new RentalService(dbFactory, authService);
    }

    public void Dispose() => connection.Dispose();

    private async Task SeedCarAsync(int id, string brand, string model, decimal dailyRate, bool isAvailable)
    {
        await using var ctx = new AppDbContext(options);
        ctx.Cars.Add(new Car
        {
            Id = id,
            Brand = brand,
            Model = model,
            Year = 2024,
            DailyRate = dailyRate,
            IsAvailable = isAvailable,
            Mileage = 1000
        });
        await ctx.SaveChangesAsync();
    }

    private async Task SeedRentalAsync(int carId, int userId, DateTime start, DateTime end, RentalStatus status)
    {
        await using var ctx = new AppDbContext(options);
        ctx.Rentals.Add(new Rental
        {
            CarId = carId,
            UserId = userId,
            StartDate = start.Date,
            EndDate = end.Date,
            Status = status,
            TotalCost = 500
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task RentCarAsync_UserNotLoggedIn_ReturnsError()
    {
        await SeedCarAsync(1, "Toyota", "Yaris", 100, true);

        string? error = await service.RentCarAsync(1, DateTime.Today, DateTime.Today.AddDays(1));

        Assert.Equal("You must be logged in.", error);
    }

    [Fact]
    public async Task RentCarAsync_StartDateInPast_ReturnsError()
    {
        await authService.RegisterAsync("customer1", "Password123");
        await SeedCarAsync(1, "Toyota", "Yaris", 100, true);
        var pastDate = DateTime.Today.AddDays(-1);

        string? error = await service.RentCarAsync(1, pastDate, DateTime.Today.AddDays(2));

        Assert.Equal("Start date cannot be in the past.", error);
    }

    [Fact]
    public async Task RentCarAsync_EndDateBeforeOrEqualStartDate_ReturnsError()
    {
        await authService.RegisterAsync("customer1", "Password123");
        await SeedCarAsync(1, "Toyota", "Yaris", 100, true);

        string? error = await service.RentCarAsync(1, DateTime.Today.AddDays(2), DateTime.Today.AddDays(1));

        Assert.Equal("End date must be later than start date.", error);
    }

    [Fact]
    public async Task RentCarAsync_PeriodTooLong_ReturnsError()
    {
        await authService.RegisterAsync("customer1", "Password123");
        await SeedCarAsync(1, "Toyota", "Yaris", 100, true);

        string? error = await service.RentCarAsync(1, DateTime.Today, DateTime.Today.AddDays(31));

        Assert.Equal("Maximum rental period is 30 days.", error);
    }

    [Fact]
    public async Task RentCarAsync_CarIsAlreadyRentedFlagFalse_ReturnsError()
    {
        await authService.RegisterAsync("customer1", "Password123");
        await SeedCarAsync(2, "BMW", "E60", 200, false);

        string? error = await service.RentCarAsync(2, DateTime.Today, DateTime.Today.AddDays(2));

        Assert.Equal("Car is already rented.", error);
    }

    [Fact]
    public async Task RentCarAsync_ValidInput_CreatesRentalAndUpdatesCarStatus()
    {
        await authService.RegisterAsync("customer1", "Password123");
        await SeedCarAsync(1, "Toyota", "Yaris", 100, true);
        var start = DateTime.Today.AddDays(1);
        var end = DateTime.Today.AddDays(4);

        string? error = await service.RentCarAsync(1, start, end);

        Assert.Null(error);

        await using var ctx = new AppDbContext(options);
        var car = await ctx.Cars.FindAsync(1);
        var rental = await ctx.Rentals.FirstOrDefaultAsync(r => r.CarId == 1);

        Assert.NotNull(car);
        Assert.False(car.IsAvailable);

        Assert.NotNull(rental);
        Assert.Equal(RentalStatus.Active, rental.Status);
        Assert.Equal(300, rental.TotalCost);
        Assert.Equal(authService.CurrentUser!.Id, rental.UserId);
    }

    [Fact]
    public async Task CancelRentalAsync_BeforeStartDate_CancelsSuccessfully()
    {
        await authService.RegisterAsync("customer1", "Password123");
        await SeedCarAsync(1, "Toyota", "Yaris", 100, false);

        int currentUserId = authService.CurrentUser!.Id;
        await SeedRentalAsync(1, currentUserId, DateTime.Today.AddDays(5), DateTime.Today.AddDays(7), RentalStatus.Active);

        await using (var ctx = new AppDbContext(options))
        {
            var rental = await ctx.Rentals.FirstAsync(r => r.CarId == 1);

  
            string? error = await service.CancelRentalAsync(rental.Id);
            Assert.Null(error);
        }

        await using var ctxVerify = new AppDbContext(options);
        var updatedRental = await ctxVerify.Rentals.FirstAsync();
        var updatedCar = await ctxVerify.Cars.FirstAsync();

        Assert.Equal(RentalStatus.Cancelled, updatedRental.Status);
        Assert.True(updatedCar.IsAvailable);
    }
}