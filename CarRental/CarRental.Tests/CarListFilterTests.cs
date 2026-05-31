using System.Globalization;
using CarRental.Models;
using CarRental.Services;
using Xunit;

namespace CarRental.Tests;

public sealed class CarListFilterTests : IDisposable
{
    private readonly CultureInfo originalCulture;

    // This shoulde be set for searching with rate ($ vs PLN)
    public CarListFilterTests()
    {
        originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = originalCulture;
    }

    [Fact]
    public void ApplySearch_EmptyQuery_ReturnsAll()
    {
        List<Car> cars = CreateCars();

        List<Car> result = CarListFilter.ApplySearch(cars, " ");

        Assert.Equal(cars.Count, result.Count);
    }

    [Fact]
    public void ApplySearch_MatchesAnyDisplayedField()
    {
        List<Car> cars = CreateCars();

        Assert.Single(CarListFilter.ApplySearch(cars, "Civic"));
        Assert.Single(CarListFilter.ApplySearch(cars, "2020"));
        Assert.Single(CarListFilter.ApplySearch(cars, "Rented"));
        Assert.Single(CarListFilter.ApplySearch(cars, "120"));
    }

    [Fact]
    public void ApplyFilters_EmptyOptions_ReturnsAll()
    {
        List<Car> cars = CreateCars();
        var options = new CarFilterOptions();

        List<Car> result = CarListFilter.ApplyFilters(cars, options);

        Assert.Equal(cars.Count, result.Count);
    }

    [Fact]
    public void ApplyFilters_ByBrandStatusYearAndMaxRate()
    {
        List<Car> cars = CreateCars();
        var options = new CarFilterOptions
        {
            MaxRate = "130"
        };
        options.SelectedBrands.Add("Toyota");
        options.SelectedStatuses.Add("Available");
        options.SelectedYears.Add("2021");

        List<Car> result = CarListFilter.ApplyFilters(cars, options);

        Car match = Assert.Single(result);
        Assert.Equal("Toyota", match.Brand);
    }

    [Fact]
    public void ApplySearchAndFilters_CombinedNarrowing()
    {
        List<Car> cars = CreateCars();
        var options = new CarFilterOptions();
        options.SelectedModels.Add("Civic");

        List<Car> result = CarListFilter.ApplySearchAndFilters(cars, "honda", options);

        Car match = Assert.Single(result);
        Assert.Equal("Honda", match.Brand);
        Assert.Equal("Civic", match.Model);
    }

    private static List<Car> CreateCars()
    {
        return new List<Car>
        {
            new() { Brand = "Toyota", Model = "Corolla", Year = 2021, DailyRate = 120m, IsAvailable = true },
            new() { Brand = "Honda", Model = "Civic", Year = 2020, DailyRate = 150m, IsAvailable = false },
            new() { Brand = "Skoda", Model = "Octavia", Year = 2022, DailyRate = 110m, IsAvailable = true }
        };
    }
}

