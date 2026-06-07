using CarRental.Data;
using CarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Services;

public class AuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public event Action? AuthStateChanged;

    public AuthService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return "Username and password are required.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        
        User? user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return "Invalid username or password.";

        CurrentUser = user;
        AuthStateChanged?.Invoke();
        return null;
    }

    public async Task<string?> RegisterAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return "Username must be at least 3 characters.";

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return "Password must be at least 6 characters.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        if (await db.Users.AnyAsync(u => u.Username == username))
            return "That username is already taken.";

        User user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Customer
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        CurrentUser = user;
        AuthStateChanged?.Invoke();
        return null;
    }

    public void Logout()
    {
        CurrentUser = null;
        AuthStateChanged?.Invoke();
    }
}
