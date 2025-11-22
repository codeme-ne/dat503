# Repository Guidelines

This repository contains a small .NET console project for power demand forecasting. Use this guide as the single source of truth when contributing manually or via AI tools.

## Project Structure & Modules
- `PowerDemandForecasting/Program.cs` and `PowerDemandForecasting.csproj`: main application entry point and project configuration.
- `PowerDemandForecasting/Data`: input data files (do not modify in-place without reason).
- `PowerDemandForecasting/Models`: saved models or artifacts produced by training.
- `PowerDemandForecasting/bin`, `PowerDemandForecasting/obj`: build outputs; never edit or commit changes here.

## Build, Run, and Development
- Build: run `dotnet build PowerDemandForecasting/PowerDemandForecasting.csproj`.
- Run: use `dotnet run --project PowerDemandForecasting/PowerDemandForecasting.csproj`.
- Clean: use `dotnet clean PowerDemandForecasting/PowerDemandForecasting.csproj` before rebuilding if you see unexpected behavior.

## Coding Style & Naming
- Language: C# with 4-space indentation; no tabs.
- Use PascalCase for classes, methods, and public properties; use camelCase for local variables and private fields.
- Prefer clear, descriptive names (e.g., `LoadTrainingData`, `ForecastNextHour`) over abbreviations.
- Keep `Program.cs` focused; extract reusable logic into new classes under `PowerDemandForecasting/`.

## Testing Guidelines
- There is currently no dedicated test project. If you add tests, create a `tests` folder and a separate `.csproj` (e.g., xUnit).
- Name test methods to describe behavior (e.g., `Forecast_ReturnsHigherValue_ForRisingTrend`).
- Run tests with `dotnet test` from the solution or test project root.

## Commit & Pull Request Practices
- Use clear, imperative commit messages (e.g., `Add baseline forecasting model`, `Refactor data loading`).
- Group related changes into a single commit; avoid mixing refactors with feature work when possible.
- Pull requests (or change descriptions) should explain motivation, summarize changes, and mention any data or model-impacting modifications.


## Agent-Specific Instructions
- Do not modify `bin/`, `obj/`, `Models/`, or raw data files unless explicitly requested.
- Prefer minimal, focused changes that align with the current structure and style.
- When adding new files, place C# source under `PowerDemandForecasting/` and keep this guideline file up to date if conventions change.

---

# Implementation & Results Documentation

This section documents the technical approach, challenges encountered, and final results of the Power Demand Forecasting project.

## 1. Methodological Approach
We implemented a univariate time-series forecasting solution using **Microsoft ML.NET** and the **Singular Spectrum Analysis (SSA)** algorithm.

- **Goal**: Forecast hourly electricity consumption (MW) for Austria.
- **Algorithm**: SSA (Forecasting via Decomposition). Decomposes the time series into trend, seasonality, and noise components.
- **Forecasting Horizon**: 24 hours ahead.

## 2. Data Pipeline & Quality Assurance

### Phase 1: Data Cleaning (`CleanData`)
- **Input**: Raw CSV with metadata headers and variable column counts.
- **Logic**:
  - Skips first 14 metadata lines (until "2016-" prefix detected).
  - Extracts columns 0 (Timestamp) and 9 (Stromverbrauch).
  - Normalizes format: Converts decimal commas to dots (`CultureInfo.InvariantCulture`).
- **Output**: `Data/el_power_clean.csv` (Strict 2-column format).

### Phase 2: Advanced Preprocessing (`HandleDstDuplicatesAndGaps`)
A critical challenge in hourly energy data is Daylight Saving Time (DST) and occasional missing records.
- **Duplicate Handling (October DST)**:
  - *Problem*: In October, the hour 02:00:00 occurs twice (clock shifts back).
  - *Solution*: Detected duplicates by timestamp. Calculated the **mean** of the two values and merged them into a single record to maintain a monotonic time series.
- **Gap Filling (March DST & Outages)**:
  - *Problem*: Missing hours in March (clock shifts forward) or other gaps.
  - *Solution*: Detected gaps > 1 hour. Used **linear interpolation** between the previous and next valid hour to fill missing values.
- **Result**: `Data/el_power_clean_dstfixed.csv` is a gap-free, monotonic time series essential for SSA.

### Phase 3: Temporal Train/Test Split
To prevent **Concept Drift** (training on outdated patterns like pre-EV/Heatpump era), we used a strict chronological split focusing on recent data:
- **Training Set**: 2023-09-30 to 2024-09-30 (1 Full Year, ~8784 records).
- **Test Set**: 2024-09-30 to 2025-09-30 (1 Full Year, ~8760 records).
- **Reasoning**: Electricity demand patterns have shifted significantly in recent years; training on 2016 data might degrade accuracy for 2024/2025.

## 3. Model Configuration (SSA)
The SSA model was configured with parameters optimized for hourly seasonality:
- **WindowSize (`168`)**: Represents one week (7 days * 24 hours). Captures weekly cycles (workdays vs. weekends).
- **SeriesLength (`720`)**: Represents one month (30 days * 24 hours). Provides sufficient context for the window sliding.
- **TrainSize**: Dynamic (matches training set size, approx. 8784 hours).
- **Confidence Level**: 95% (Lower/Upper bounds generated).

## 4. Results & Evaluation
The model was evaluated on the unseen Test Set (2024-2025).

| Metric | Value | Interpretation |
|--------|-------|----------------|
| **Mean Load** | 6662.50 MW | Average consumption in test period. |
| **MAE (Mean Absolute Error)** | **261.16 MW** | On average, the forecast is off by ~261 MW. |
| **MAE %** | **3.92 %** | Relative error is below 4%, indicating high accuracy. |
| **RMSE (Root Mean Squared Error)** | **339.40 MW** | Slightly higher than MAE, indicating some outlier errors but overall stable. |

**Conclusion**: The SSA model provides a robust baseline forecast with < 5% error margin, suitable for operational planning.

## 5. Known Issues & Future Improvements
- **Metric**: Current error is low, but extreme weather events might cause higher deviations.
- **Future Work**:
  - Integrate external regressors (Temperature, Holidays) if switching to algorithms that support it (SSA is univariate).
  - Extend forecast horizon beyond 24h using recursive forecasting.
