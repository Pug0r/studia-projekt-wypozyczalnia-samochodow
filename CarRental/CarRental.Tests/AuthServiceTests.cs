using CarRental.Data;
using CarRental.Models;
using CarRental.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRental.Tests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<AppDbContext> rename;
    private readonly AuthService service;

    public AuthServiceTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        rename = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        using var ctx = new AppDbContext(rename);
        ctx.Database.EnsureCreated();

        service = new AuthService(new TestDbContextFactory(rename));
    }

    public void Dispose() => connection.Dispose();

    private async Task SeedUserAsync(string username, string password)
    {
        await using var ctx = new AppDbContext(rename);
        ctx.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Customer
        });
        await ctx.SaveChangesAsync();
    }
    
    [Fact]
    public void InitialState_IsNotAuthenticated()
    {
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.CurrentUser);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("   ", "password")]
    [InlineData("user", "")]
    public async Task LoginAsync_MissingCredentials_ReturnsError(string username, string password)
    {
        string? error = await service.LoginAsync(username, password);

        Assert.NotNull(error);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_WrongCredentials_ReturnsError()
    {
        await SeedUserAsync("alice", "correctPassword");

        Assert.NotNull(await service.LoginAsync("nobody", "correctPassword"));
        Assert.NotNull(await service.LoginAsync("alice", "wrongPassword"));
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_SetsAuthState()
    {
        await SeedUserAsync("alice", "secret123");
        int eventCount = 0;
        service.AuthStateChanged += () => eventCount++;

        string? error = await service.LoginAsync("alice", "secret123");

        Assert.Null(error);
        Assert.True(service.IsAuthenticated);
        Assert.Equal("alice", service.CurrentUser!.Username);
        Assert.Equal(1, eventCount);
    }
    
    [Theory]
    [InlineData("ab", "password123")]
    [InlineData("alice", "12345")]
    [InlineData("", "password123")]
    [InlineData("alice", "")]
    public async Task RegisterAsync_InvalidInput_ReturnsError(string username, string password)
    {
        string? error = await service.RegisterAsync(username, password);

        Assert.NotNull(error);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ReturnsError()
    {
        await SeedUserAsync("alice", "password123");

        string? error = await service.RegisterAsync("alice", "newPassword");

        Assert.NotNull(error);
    }

    [Fact]
    public async Task RegisterAsync_ValidInput_SetsAuthState()
    {
        int eventCount = 0;
        service.AuthStateChanged += () => eventCount++;

        string? error = await service.RegisterAsync("newuser", "password123");

        Assert.Null(error);
        Assert.True(service.IsAuthenticated);
        Assert.Equal("newuser", service.CurrentUser!.Username);
        Assert.Equal(UserRole.Customer, service.CurrentUser.Role);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task Logout_ClearsAuthState()
    {
        await SeedUserAsync("alice", "secret123");
        await service.LoginAsync("alice", "secret123");
        int eventCount = 0;
        service.AuthStateChanged += () => eventCount++;

        service.Logout();

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.CurrentUser);
        Assert.Equal(1, eventCount);
    }
    
    [Fact]
    public async Task RegisteredUser_CanLoginAfterRegistration()
    {
        await service.RegisterAsync("newuser", "password123");
        service.Logout();

        string? error = await service.LoginAsync("newuser", "password123");

        Assert.Null(error);
        Assert.True(service.IsAuthenticated);
    }
}
