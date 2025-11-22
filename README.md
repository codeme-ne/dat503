# Power Demand Forecasting (Austria)

.NET 8 Console App that forecasts hourly electricity consumption for Austria with **ML.NET** and **Singular Spectrum Analysis (SSA)**. Data source: [E-Control](https://www.e-control.at/statistik/e-statistik/data).

## Quick Start
- Prereq: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Build: `dotnet build PowerDemandForecasting/PowerDemandForecasting.csproj`
- Run full pipeline: `dotnet run --project PowerDemandForecasting/PowerDemandForecasting.csproj`
- Outputs land in `PowerDemandForecasting/Data/` (cleaned data, splits, evaluation CSV) and `PowerDemandForecasting/Models/forecast_model.zip` (trained model).

## Repo Layout
- `PowerDemandForecasting/Program.cs` — orchestrates all pipeline steps.
- `PowerDemandForecasting/Data/` — input `el_dataset_h.csv` plus generated CSVs (clean, DST-fixed, train/test, evaluation).
- `PowerDemandForecasting/Models/` — DTOs (`ModelInput`, `ModelOutput`) and saved model artifact.
- `bin/`, `obj/` — build outputs (nicht bearbeiten/committen).

## Pipeline (kurz)
1) **CleanData** — liest `el_dataset_h.csv`, zieht Spalte 0 (Timestamp) & 9 (Stromverbrauch), normiert Dezimaltrennzeichen, schreibt `el_power_clean.csv`.  
2) **QualityChecks** — prüft Duplikate, Monotonie, NaN/negativ/0 sowie Verteilung.  
3) **HandleDstDuplicatesAndGaps** — mittelt doppelte Oktober-Stunden, interpoliert fehlende März-Stunden → `el_power_clean_dstfixed.csv`.  
4) **CreateTrainTestFiles** — chronologischer Split: Train 30.09.2023–30.09.2024, Test 30.09.2024–30.09.2025 → `train_data.csv`, `test_data.csv`.  
5) **TrainModel** — SSA: `windowSize=168`, `seriesLength=720`, `horizon=24`, `confidenceLevel=0.95` → `forecast_model.zip`.  
6) **EvaluateAndExport** — berechnet MAE/RMSE, schreibt `evaluation_details.csv` mit Forecast + Bounds.

## Ergebnisse (Testperiode Sept 2024–Sept 2025)
- MAE: 261.16 MW (~3.9 %)
- RMSE: 339.40 MW
- Mean Load: 6662.50 MW
→ Robust < 5 % Fehler für kurzfristige Planung.

## Hinweise
- Keine manuellen Änderungen in `bin/`, `obj/`, `Models/forecast_model.zip` oder Rohdaten; Daten immer über die Pipeline erzeugen.  
- Bei unerwartetem Verhalten: `dotnet clean PowerDemandForecasting/PowerDemandForecasting.csproj` ausführen und erneut bauen/laufen lassen.
