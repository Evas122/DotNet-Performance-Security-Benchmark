# DotNet-Performance-Security-Benchmark

## EF Core migrations (Package Manager Console)

W Package Manager Console wykonaj (upewnij się, że `Default project` ustawione jest na `SecPerf.Infrastructure` lub podaj `-Project`):

Add-Migration InitialCreate -Project SecPerf.Infrastructure -StartupProject SecPerf.Api.Minimal -Context ApplicationDbContext

Update-Database -Project SecPerf.Infrastructure -StartupProject SecPerf.Api.Minimal -Context ApplicationDbContext

Lub z użyciem dotnet CLI:

dotnet ef migrations add InitialCreate --project src/SecPerf.Infrastructure --startup-project src/SecPerf.Api.Minimal --context ApplicationDbContext

dotnet ef database update --project src/SecPerf.Infrastructure --startup-project src/SecPerf.Api.Minimal --context ApplicationDbContext

`--project` wskazuje projekt z DbContext/migracjami (SecPerf.Infrastructure). `--startup-project` to projekt startowy (np. SecPerf.Api.Minimal).

## Seeder (uruchomienie generatora danych)

Projekt runnera: `src/SecPerf.Seeder`.

1. Zastosuj migracje (update-database).
2. Uruchom seeda:

dotnet run --project src/SecPerf.Seeder

Runner używa `appsettings.Development.json` jeśli istnieje; możesz nadpisać connection string w PowerShell:

$env:ConnectionStrings__DefaultConnection = 'Server=...;Database=...;User Id=...;Password=...;'

dotnet run --project src/SecPerf.Seeder

Domyślne rozmiary datasetu (można zmienić w seedach):
- Users: 10_000
- Products: 10_000
- Orders: 100_000

