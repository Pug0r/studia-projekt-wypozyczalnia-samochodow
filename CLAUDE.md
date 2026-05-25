# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project purpose

A university desktop car rental management system (**wypożyczalnia samochodów**). The primary goal is functional completeness — a working UI the lecturer can operate with demo credentials. The stack is **Photino + Blazor (Razor)** on .NET 8, backed by **EF Core + SQLite**.

Two user roles:
- **Administrator** — full management of cars, users, rentals
- **Customer** (renter) — must log in; can search/filter cars, view their own rental history

Required features (to be implemented):
- Car search and filtering
- Rental history per user
- Mileage and defect tracking
- Rental count statistics per user
- Unit tests (minimum requirement)
- User-facing tutorial/description document

Demo credentials must be simple and well-known (e.g. `haslo123`) so the lecturer can log in without setup.

## Commands

```bash
# FIRST HAD TO DO
sudo apt install libwebkit2gtk-4.1-0
# Build entire solution
dotnet build CarRental.sln

# Run the desktop app (opens a native window)
dotnet run --project src/CarRental.Desktop/CarRental.Desktop.csproj

# Run tests (once a test project exists)
dotnet test CarRental.sln
dotnet test CarRental.sln --filter "FullyQualifiedName~SomeTest"

# Publish self-contained single-file release for submission
dotnet publish src/CarRental.Desktop/CarRental.Desktop.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

## Architecture

```
CarRental.Core          ← no dependencies; domain entities only
CarRental.Infrastructure ← depends on Core; EF Core + SQLite
CarRental.Desktop       ← depends on Infrastructure; Photino + Blazor UI
```

**`CarRental.Core`** — plain C# entity classes with Data Annotations for validation. No EF references. Add new domain entities (Car, Rental, Defect, …) here.

**`CarRental.Infrastructure`** — `CarRentalDbContext` (EF Core), SQLite. The `DependencyInjection.AddInfrastructure()` extension registers the DB context. New entities must be added as `DbSet<T>` and configured in `OnModelCreating`. Default DB file: `car-rental.db` in the working directory.

**`CarRental.Desktop`** — Photino renders a native window that hosts a Blazor app (not a web browser). Entry point is `Program.cs`:
- Calls `EnsureCreated()` at startup to auto-create the SQLite schema — **no migrations needed**.
- Window is fixed at 1000×700, `InvariantGlobalization=true`.
- Blazor routing uses `@page` directives; `App.razor` is the router root with `MainLayout` as the default layout.
- Global Razor imports are in `_Imports.razor`.
- Styles live in `wwwroot/css/app.css`.

## Current skeleton state

Only `AppUser` entity and the `/` Login page exist. Still to be built: Car/Rental/Defect entities, service/repository layer, all feature pages, authentication state, navigation shell, seed data with demo credentials, and a test project.

When adding a service/repository layer, register it in `DependencyInjection.cs` and inject it into Razor pages via `@inject`.