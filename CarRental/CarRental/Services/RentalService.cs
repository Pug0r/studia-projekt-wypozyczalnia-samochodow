using CarRental.Data;
using CarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Services;

public sealed class RentalService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthService _authService;

    public RentalService(IDbContextFactory<AppDbContext> dbFactory, AuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public async Task<List<Rental>> GetMyRentalsAsync()
    {
        if (_authService.CurrentUser == null)
            return new List<Rental>();

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        return await db.Rentals
            .Include(r => r.Car)
            .Where(r => r.UserId == _authService.CurrentUser.Id)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();
    }

    public async Task<List<Rental>> GetAllRentalsAsync()
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        return await db.Rentals
            .Include(r => r.Car)
            .Include(r => r.User)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();
    }

    public async Task<List<Rental>> GetActiveRentalsAsync()
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        return await db.Rentals
            .Include(r => r.Car)
            .Include(r => r.User)
            .Where(r => r.Status == RentalStatus.Active)
            .OrderBy(r => r.EndDate)
            .ToListAsync();
    }

    public async Task<string?> RentCarAsync(int carId, DateTime startDate, DateTime endDate)
    {
        if (_authService.CurrentUser == null)
            return "You must be logged in.";

        if (startDate.Date < DateTime.Today)
            return "Start date cannot be in the past.";

        if (endDate.Date <= startDate.Date)
            return "End date must be later than start date.";

        if ((endDate.Date - startDate.Date).Days > 30)
            return "Maximum rental period is 30 days.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        Car? car = await db.Cars.FindAsync(carId);
        if (car == null)
            return "Car does not exist.";

        if (!car.IsAvailable)
            return "Car is already rented.";

        bool hasConflict = await db.Rentals.AnyAsync(r =>
            r.CarId == carId &&
            r.Status == RentalStatus.Active &&
            r.StartDate.Date <= endDate.Date &&
            r.EndDate.Date >= startDate.Date);

        if (hasConflict)
            return "Car is already booked for this period.";


        int days = (endDate.Date - startDate.Date).Days;
        decimal totalCost = days * car.DailyRate;

        Rental rental = new Rental
        {
            CarId = carId,
            UserId = _authService.CurrentUser.Id,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalCost = totalCost,
            Status = RentalStatus.Active
        };


        car.IsAvailable = false;

        db.Rentals.Add(rental);
        await db.SaveChangesAsync();

        return null; 
    }

    public async Task<string?> ReturnCarAsync(int rentalId)
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        Rental? rental = await db.Rentals
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.Id == rentalId);

        if (rental == null)
            return "Rental does not exist.";

        if (rental.Status != RentalStatus.Active)
            return "This rental has already been completed.";

        if (_authService.CurrentUser?.Role != UserRole.Admin &&
            rental.UserId != _authService.CurrentUser?.Id)
            return "You do not have permission to return this car.";

        rental.ReturnDate = DateTime.Now;
        rental.Status = RentalStatus.Completed;

        if (rental.Car != null)
            rental.Car.IsAvailable = true;

        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> CancelRentalAsync(int rentalId)
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        Rental? rental = await db.Rentals
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.Id == rentalId);

        if (rental == null)
            return "Rental does not exist.";

        if (rental.Status != RentalStatus.Active)
            return "This rental cannot be canceled.";

        bool isAdmin = _authService.CurrentUser?.Role == UserRole.Admin;
        bool isOwner = rental.UserId == _authService.CurrentUser?.Id;

        if (!isAdmin && !isOwner)
            return "You do not have permission to cancel this rental.";

        if (!isAdmin && rental.StartDate.Date <= DateTime.Today)
            return "You cannot cancel a rental that has already started.";

        rental.Status = RentalStatus.Cancelled;

        if (rental.Car != null)
            rental.Car.IsAvailable = true;

        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> ExtendRentalAsync(int rentalId, DateTime newEndDate)
    {
        if (_authService.CurrentUser?.Role != UserRole.Admin)
            return "Only administrators can extend rentals.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        Rental? rental = await db.Rentals
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.Id == rentalId);

        if (rental == null)
            return "Rental does not exist.";

        if (rental.Status != RentalStatus.Active)
            return "This rental is not active.";

        if (newEndDate.Date <= rental.EndDate.Date)
            return "New end date must be later.";

        if ((newEndDate.Date - rental.StartDate.Date).Days > 45)
            return "Maximum rental period is 45 days.";

        bool hasConflict = await db.Rentals.AnyAsync(r =>
            r.CarId == rental.CarId &&
            r.Id != rentalId &&
            r.Status == RentalStatus.Active &&
            r.StartDate.Date <= newEndDate.Date &&
            r.EndDate.Date >= rental.EndDate.Date);

        if (hasConflict)
            return "New end date conflicts with another rental.";

        int additionalDays = (newEndDate.Date - rental.EndDate.Date).Days;
        if (rental.Car != null)
        {
            rental.TotalCost += additionalDays * rental.Car.DailyRate;
        }

        rental.EndDate = newEndDate.Date;

        await db.SaveChangesAsync();
        return null;
    }
}