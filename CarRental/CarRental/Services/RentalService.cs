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

    // Pobierz wszystkie wypożyczenia bieżącego użytkownika
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

    // Pobierz wszystkie wypożyczenia (dla admina)
    public async Task<List<Rental>> GetAllRentalsAsync()
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();
        return await db.Rentals
            .Include(r => r.Car)
            .Include(r => r.User)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();
    }

    // Pobierz aktywne wypożyczenia (admin)
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

    // Wypożycz samochód
    public async Task<string?> RentCarAsync(int carId, DateTime startDate, DateTime endDate)
    {
        // Walidacja
        if (_authService.CurrentUser == null)
            return "Musisz być zalogowany.";

        if (startDate.Date < DateTime.Today)
            return "Data rozpoczęcia nie może być w przeszłości.";

        if (endDate.Date <= startDate.Date)
            return "Data zakończenia musi być późniejsza niż data rozpoczęcia.";

        if ((endDate.Date - startDate.Date).Days > 30)
            return "Maksymalny okres wypożyczenia to 30 dni.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        // Sprawdź czy samochód istnieje i jest dostępny
        Car? car = await db.Cars.FindAsync(carId);
        if (car == null)
            return "Samochód nie istnieje.";

        if (!car.IsAvailable)
            return "Samochód jest już wypożyczony.";

        // Sprawdź czy nie ma konfliktów terminów
        bool hasConflict = await db.Rentals.AnyAsync(r =>
            r.CarId == carId &&
            r.Status == RentalStatus.Active &&
            r.StartDate.Date <= endDate.Date &&
            r.EndDate.Date >= startDate.Date);

        if (hasConflict)
            return "Samochód jest już zarezerwowany w tym terminie.";

        // Oblicz koszt
        int days = (endDate.Date - startDate.Date).Days;
        decimal totalCost = days * car.DailyRate;

        // Utwórz wypożyczenie
        Rental rental = new Rental
        {
            CarId = carId,
            UserId = _authService.CurrentUser.Id,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalCost = totalCost,
            Status = RentalStatus.Active
        };

        // Zaktualizuj dostępność samochodu
        car.IsAvailable = false;

        db.Rentals.Add(rental);
        await db.SaveChangesAsync();

        return null; // Sukces
    }

    // Zwróć samochód
    public async Task<string?> ReturnCarAsync(int rentalId)
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        Rental? rental = await db.Rentals
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.Id == rentalId);

        if (rental == null)
            return "Wypożyczenie nie istnieje.";

        if (rental.Status != RentalStatus.Active)
            return "To wypożyczenie zostało już zakończone.";

        // Sprawdź uprawnienia (admin lub właściciel)
        if (_authService.CurrentUser?.Role != UserRole.Admin &&
            rental.UserId != _authService.CurrentUser?.Id)
            return "Nie masz uprawnień do zwrotu tego samochodu.";

        rental.ReturnDate = DateTime.Now;
        rental.Status = RentalStatus.Completed;

        // Przywróć dostępność samochodu
        if (rental.Car != null)
            rental.Car.IsAvailable = true;

        await db.SaveChangesAsync();
        return null;
    }

    // Anuluj wypożyczenie (tylko dla admina lub przed rozpoczęciem)
    public async Task<string?> CancelRentalAsync(int rentalId)
    {
        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        Rental? rental = await db.Rentals
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.Id == rentalId);

        if (rental == null)
            return "Wypożyczenie nie istnieje.";

        if (rental.Status != RentalStatus.Active)
            return "To wypożyczenie nie może być anulowane.";

        // Sprawdź uprawnienia
        bool isAdmin = _authService.CurrentUser?.Role == UserRole.Admin;
        bool isOwner = rental.UserId == _authService.CurrentUser?.Id;

        if (!isAdmin && !isOwner)
            return "Nie masz uprawnień do anulowania tego wypożyczenia.";

        // Jeśli anuluje klient, może to zrobić tylko przed rozpoczęciem
        if (!isAdmin && rental.StartDate.Date <= DateTime.Today)
            return "Nie możesz anulować wypożyczenia, które już się rozpoczęło.";

        rental.Status = RentalStatus.Cancelled;

        // Przywróć dostępność samochodu
        if (rental.Car != null)
            rental.Car.IsAvailable = true;

        await db.SaveChangesAsync();
        return null;
    }

    // Przedłuż wypożyczenie (admin)
    public async Task<string?> ExtendRentalAsync(int rentalId, DateTime newEndDate)
    {
        if (_authService.CurrentUser?.Role != UserRole.Admin)
            return "Tylko administrator może przedłużać wypożyczenia.";

        await using AppDbContext db = await _dbFactory.CreateDbContextAsync();

        Rental? rental = await db.Rentals
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.Id == rentalId);

        if (rental == null)
            return "Wypożyczenie nie istnieje.";

        if (rental.Status != RentalStatus.Active)
            return "To wypożyczenie nie jest aktywne.";

        if (newEndDate.Date <= rental.EndDate.Date)
            return "Nowa data zakończenia musi być późniejsza.";

        if ((newEndDate.Date - rental.StartDate.Date).Days > 45)
            return "Maksymalny łączny okres wypożyczenia to 45 dni.";

        // Sprawdź konflikty
        bool hasConflict = await db.Rentals.AnyAsync(r =>
            r.CarId == rental.CarId &&
            r.Id != rentalId &&
            r.Status == RentalStatus.Active &&
            r.StartDate.Date <= newEndDate.Date &&
            r.EndDate.Date >= rental.EndDate.Date);

        if (hasConflict)
            return "Nowy termin koliduje z innym wypożyczeniem.";

        // Oblicz dodatkowy koszt
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