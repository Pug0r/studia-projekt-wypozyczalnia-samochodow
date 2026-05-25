using CarRental.Infrastructure;
using CarRental.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CarRental.Desktop;

public partial class AppHost : Application
{
    public IServiceProvider Services { get; }

    public AppHost()
    {
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddInfrastructure();

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
        dbContext.Database.EnsureCreated();
    }
}
