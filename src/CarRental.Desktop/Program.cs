using CarRental.Infrastructure;
using CarRental.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

builder.Services.AddInfrastructure();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MainWindow.SetTitle("Car Rental").SetUseOsDefaultSize(false).SetSize(1000, 700);
app.RootComponents.Add(typeof(CarRental.Desktop.App), "#app");

app.Run();
