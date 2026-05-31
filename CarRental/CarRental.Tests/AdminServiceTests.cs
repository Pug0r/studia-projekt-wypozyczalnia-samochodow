using CarRental.Data;
using CarRental.Models;
using CarRental.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.Tests;

public sealed class AdminServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<AppDbContext> options;
    private readonly AdminService service;

    public AdminServiceTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        using var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();

        service = new AdminService(new TestDbContextFactory(options));
    }

    public void Dispose() => connection.Dispose();

    private async Task<int> SeedCarAsync(string make = "Toyota", string model = "Corolla",
        int year = 2022, decimal rate = 100m)
    {
        await using var ctx = new AppDbContext(options);
        var car = new Car { Brand = make, Model = model, Year = year, DailyRate = rate, IsAvailable = true };
        ctx.Cars.Add(car);
        await ctx.SaveChangesAsync();
        return car.Id;
    }

    private async Task<int> SeedUserAsync(string username = "alice", UserRole role = UserRole.Customer)
    {
        await using var ctx = new AppDbContext(options);
        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = role
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task GetAllCarsAsync_EmptyDb_ReturnsEmptyList()
    {
        List<Car> result = await service.GetAllCarsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddCarAsync_ValidCar_ReturnsNullAndCarAppears()
    {
        string? error = await service.AddCarAsync("Honda", "Civic", 2023, 99.99m, true, 15000, "", null, null, null);

        Assert.Null(error);
        List<Car> cars = await service.GetAllCarsAsync();
        Assert.Single(cars);
        Assert.Equal("Honda", cars[0].Brand);
        Assert.Equal("Civic", cars[0].Model);
        Assert.Equal(15000, cars[0].Mileage);
    }

    [Fact]
    public async Task AddCarAsync_WithImage_StoresImageData()
    {
        byte[] image = { 1, 2, 3, 4 };
        string? error = await service.AddCarAsync("Mazda", "3", 2021, 89.5m, true, 12000, "",
            image, "image/png", "mazda.png");

        Assert.Null(error);
        Car car = Assert.Single(await service.GetAllCarsAsync());
        Assert.Equal(image, car.ImageData);
        Assert.Equal("image/png", car.ImageContentType);
        Assert.Equal("mazda.png", car.ImageFileName);
    }

    [Theory]
    [InlineData("", "Corolla", 2022, 100, 0)]
    [InlineData("Toyota", "", 2022, 100, 0)]
    [InlineData("Toyota", "Corolla", 0, 100, 0)]
    [InlineData("Toyota", "Corolla", 2022, -1, 0)]
    [InlineData("Toyota", "Corolla", 2022, 100, -1)]
    public async Task AddCarAsync_InvalidInput_ReturnsError(string make, string model, int year, decimal rate, int mileage)
    {
        string? error = await service.AddCarAsync(make, model, year, rate, true, mileage, "", null, null, null);

        Assert.NotNull(error);
        Assert.Empty(await service.GetAllCarsAsync());
    }

    [Fact]
    public async Task UpdateCarAsync_ExistingCar_UpdatesAllFields()
    {
        int id = await SeedCarAsync("Toyota", "Corolla", 2020, 100m);

        string? error = await service.UpdateCarAsync(id, "Honda", "Civic", 2023, 149.99m, 30000, "Cracked mirror",
            new byte[] { 5, 6, 7 }, "image/jpeg", "honda.jpg");

        Assert.Null(error);
        List<Car> cars = await service.GetAllCarsAsync();
        Car updated = Assert.Single(cars);
        Assert.Equal("Honda", updated.Brand);
        Assert.Equal("Civic", updated.Model);
        Assert.Equal(2023, updated.Year);
        Assert.Equal(149.99m, updated.DailyRate);
        Assert.Equal(30000, updated.Mileage);
        Assert.Equal("Cracked mirror", updated.Faults);
        Assert.Equal("image/jpeg", updated.ImageContentType);
        Assert.Equal("honda.jpg", updated.ImageFileName);
        Assert.Equal(new byte[] { 5, 6, 7 }, updated.ImageData);
    }

    [Fact]
    public async Task UpdateCarAsync_NonExistentCar_ReturnsError()
    {
        string? error = await service.UpdateCarAsync(999, "Honda", "Civic", 2023, 100m, 0, "", null, null, null);

        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("", "Corolla", 2022, 100, 0)]
    [InlineData("Toyota", "", 2022, 100, 0)]
    [InlineData("Toyota", "Corolla", 0, 100, 0)]
    [InlineData("Toyota", "Corolla", 2022, -1, 0)]
    [InlineData("Toyota", "Corolla", 2022, 100, -1)]
    public async Task UpdateCarAsync_InvalidInput_ReturnsError(string make, string model, int year, decimal rate, int mileage)
    {
        int id = await SeedCarAsync();

        string? error = await service.UpdateCarAsync(id, make, model, year, rate, mileage, "", null, null, null);

        Assert.NotNull(error);
        Car original = Assert.Single(await service.GetAllCarsAsync());
        Assert.Equal("Toyota", original.Brand);
    }

    [Fact]
    public async Task DeleteCarAsync_ExistingCar_RemovesIt()
    {
        int id = await SeedCarAsync();

        string? error = await service.DeleteCarAsync(id);

        Assert.Null(error);
        Assert.Empty(await service.GetAllCarsAsync());
    }

    [Fact]
    public async Task GetAllUsersAsync_WithSeededUser_ReturnsIt()
    {
        await SeedUserAsync("bob");

        List<User> result = await service.GetAllUsersAsync();

        Assert.Single(result);
        Assert.Equal("bob", result[0].Username);
    }

    [Fact]
    public async Task AddUserAsync_ValidUser_ReturnsNullAndUserAppears()
    {
        string? error = await service.AddUserAsync("newuser", "password123", UserRole.Customer);

        Assert.Null(error);
        List<User> users = await service.GetAllUsersAsync();
        Assert.Single(users);
        Assert.Equal("newuser", users[0].Username);
        Assert.Equal(UserRole.Customer, users[0].Role);
    }

    [Fact]
    public async Task AddUserAsync_DuplicateUsername_ReturnsError()
    {
        await SeedUserAsync("alice");

        string? error = await service.AddUserAsync("alice", "password123", UserRole.Customer);

        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("ab", "password123")]
    [InlineData("alice", "12345")]
    public async Task AddUserAsync_InvalidInput_ReturnsError(string username, string password)
    {
        string? error = await service.AddUserAsync(username, password, UserRole.Customer);

        Assert.NotNull(error);
        Assert.Empty(await service.GetAllUsersAsync());
    }

    [Fact]
    public async Task DeleteUserAsync_OtherUser_RemovesIt()
    {
        int adminId = await SeedUserAsync("admin", UserRole.Admin);
        int userId = await SeedUserAsync("alice");

        string? error = await service.DeleteUserAsync(userId, requestingUserId: adminId);

        Assert.Null(error);
        List<User> remaining = await service.GetAllUsersAsync();
        Assert.DoesNotContain(remaining, u => u.Id == userId);
    }

    [Fact]
    public async Task DeleteUserAsync_SelfDelete_ReturnsError()
    {
        int adminId = await SeedUserAsync("admin", UserRole.Admin);

        string? error = await service.DeleteUserAsync(adminId, requestingUserId: adminId);

        Assert.NotNull(error);
        Assert.Single(await service.GetAllUsersAsync());
    }
}
