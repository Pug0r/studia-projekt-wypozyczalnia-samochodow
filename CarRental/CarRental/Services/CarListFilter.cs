using CarRental.Models;

namespace CarRental.Services;

public sealed class CarFilterOptions
{
    public HashSet<string> SelectedBrands { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SelectedModels { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SelectedYears { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SelectedStatuses { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string MaxRate { get; set; } = string.Empty;
}

public static class CarListFilter
{
    public static List<Car> ApplySearchAndFilters(List<Car> source, string searchQuery, CarFilterOptions options)
    {
        List<Car> searched = ApplySearch(source, searchQuery);
        return ApplyFilters(searched, options);
    }

    public static List<Car> ApplySearch(List<Car> source, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source;

        string q = query.Trim();
        return source.Where(car => MatchesQuery(car, q)).ToList();
    }

    public static List<Car> ApplyFilters(List<Car> source, CarFilterOptions options)
    {
        IEnumerable<Car> query = source;

        if (options.SelectedBrands.Count > 0)
            query = query.Where(car => options.SelectedBrands.Contains(car.Brand));

        if (options.SelectedModels.Count > 0)
            query = query.Where(car => options.SelectedModels.Contains(car.Model));

        if (options.SelectedYears.Count > 0)
            query = query.Where(car => options.SelectedYears.Contains(car.Year.ToString()));

        if (options.SelectedStatuses.Count > 0)
            query = query.Where(car => options.SelectedStatuses.Contains(car.IsAvailable ? "Available" : "Rented"));

        if (decimal.TryParse(options.MaxRate, out decimal maxRate))
            query = query.Where(car => car.DailyRate <= maxRate);

        return query.ToList();
    }

    public static bool MatchesQuery(Car car, string query)
    {
        return ContainsIgnoreCase(car.Brand, query)
               || ContainsIgnoreCase(car.Model, query)
               || car.Year.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
               || car.DailyRate.ToString("C").Contains(query, StringComparison.OrdinalIgnoreCase)
               || car.DailyRate.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
               || ContainsIgnoreCase(car.IsAvailable ? "Available" : "Rented", query);
    }

    private static bool ContainsIgnoreCase(string? value, string query)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}

