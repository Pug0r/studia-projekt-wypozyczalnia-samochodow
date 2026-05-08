# studia-projekt-wypozyczalnia-samochodow

Skeleton desktop app for a car rental system built with Razor/Blazor components.

## Tech stack

- Desktop host: Photino + Blazor (Razor UI)
- Data access: Entity Framework Core + SQLite
- Solution structure prepared for team development

## Project structure

- `src/CarRental.Core` - domain entities (shared, no business logic)
- `src/CarRental.Infrastructure` - database context and infrastructure wiring
- `src/CarRental.Desktop` - desktop application host and Razor views

## Run locally

```bash
dotnet build CarRental.slnx
dotnet run --project /home/runner/work/studia-projekt-wypozyczalnia-samochodow/studia-projekt-wypozyczalnia-samochodow/src/CarRental.Desktop/CarRental.Desktop.csproj
```

The app currently includes one example view: login page (`/`).

## Publish self-contained single-file app

Windows (.exe):

```bash
dotnet publish /home/runner/work/studia-projekt-wypozyczalnia-samochodow/studia-projekt-wypozyczalnia-samochodow/src/CarRental.Desktop/CarRental.Desktop.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

macOS:

```bash
dotnet publish /home/runner/work/studia-projekt-wypozyczalnia-samochodow/studia-projekt-wypozyczalnia-samochodow/src/CarRental.Desktop/CarRental.Desktop.csproj -c Release -r osx-x64 -p:PublishSingleFile=true -p:SelfContained=true
```
