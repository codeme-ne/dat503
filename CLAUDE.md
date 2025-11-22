# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an **ML.NET 8** console application that forecasts **hourly electricity consumption** for Austria using **SSA (Singular Spectrum Analysis)** time series forecasting. The architecture is based on Microsoft's Bike Sharing Demand tutorial but adapted for:
- CSV data instead of SQL Server
- Hourly granularity instead of daily
- Electricity consumption (MW) instead of bike rentals

**Current Status:** Phases 0-5 complete (data cleaning, loading, train/test split, SSA training, evaluation). Phase 6+ pending (documentation finalization, future forecasting).

**Reference files:**
- `PLAN.md` - Comprehensive 10-phase implementation plan (German)
- `AGENTS.md` - Repository coding guidelines
- `Bike_Example_Github_Repo.txt` - Original ML.NET bike sharing example
- `Data/el_dataset_h.csv` - Raw hourly electricity data from Austria
- `SSA_encyclopedia.pdf` - SSA algorithm background

## Essential Commands

All commands must be run from the **PowerDemandForecasting/** directory:

```bash
# Navigate to project directory
cd PowerDemandForecasting

# Build the project
dotnet build

# Run the application
dotnet run

# Clean and rebuild
dotnet clean && dotnet build

# Run with specific configuration
dotnet run --configuration Release
```

## Project Structure

```
PowerDemandForecasting/
├── Data/
│   ├── el_dataset_h.csv              # Raw data (semicolon-separated, comma decimals)
│   ├── el_power_clean.csv            # Auto-generated: cleaned data
│   ├── el_power_clean_dstfixed.csv   # Auto-generated: DST-corrected data
│   ├── train_data.csv                # Auto-generated: training split
│   ├── test_data.csv                 # Auto-generated: testing split
│   └── evaluation_details.csv        # Auto-generated: evaluation results (pending)
├── Models/
│   ├── ModelInput.cs                 # Input DTO (namespace: PowerDemandForecasting.Models)
│   └── ModelOutput.cs                # Output DTO (namespace: PowerDemandForecasting.Models)
├── Program.cs                        # Main application entry point
├── PowerDemandForecasting.csproj     # Project file (.NET 8, ML.NET 5.0.0)
└── MLModel.zip                       # Auto-generated: trained model (pending)
```

## Data Architecture

### Raw Data Structure (`el_dataset_h.csv`)
- **Separator:** `;` (semicolon)
- **Decimal:** `,` (comma in raw data)
- **First ~14 lines:** Metadata headers (skip these)
- **Column 0:** Timestamp (`"2016-01-01 00:00:00"`)
- **Column 9:** `Stromverbrauch` (electricity consumption in MW) - **THIS IS THE TARGET**
- **Column 1:** `Inlandstromverbrauch` - EMPTY in data rows, do NOT use

### Data Processing Pipeline

**Phase 1: CleanData()** - `Program.cs:58-191`
- Reads `el_dataset_h.csv` → produces `Data/el_power_clean.csv`
- Skips metadata headers (detects first line starting with `"2016-"`)
- Extracts only columns 0 (Timestamp) and 9 (Stromverbrauch)
- Converts decimal separator `,` → `.`
- Validates timestamps and values (skips NaN/empty/invalid)
- Output format: `Timestamp;Stromverbrauch` with header
- Uses `CultureInfo.InvariantCulture` throughout

**Phase 2: LoadData() + PerformQualityChecks()** - `Program.cs:219-384`
- TextLoader reads cleaned CSV with:
  - `Separators = new[] { ';' }`
  - `DecimalMarker = '.'` (already converted by CleanData)
  - `HasHeader = true`
- Quality checks validate:
  - Date range, NaN/negative values, duplicates
  - Timestamp monotonicity
  - Distribution statistics (min/max/mean/stddev)

**Phase 3: HandleDstDuplicatesAndGaps()** - `Program.cs:386-556`
- **CRITICAL DST CORRECTION LOGIC:**
  - **October duplicates** (DST "fall back"): Detects duplicate timestamps in October, merges by averaging values
  - **March gaps** (DST "spring forward"): Detects missing hours in March, interpolates using linear method
  - Creates `Data/el_power_clean_dstfixed.csv` (non-destructive, preserves intermediate files)
  - Final integrity check ensures no gaps/duplicates remain

**Phase 4: CreateTrainTestFiles()** - `Program.cs:557-626`
- Creates physical CSV files for train/test splits
- Output: `Data/train_data.csv` and `Data/test_data.csv`
- See temporal split strategy below

### Temporal Split Strategy (Concept Drift Prevention)

**CRITICAL:** To avoid concept drift (outdated patterns pre-dating EVs, heat pumps), only train on recent data:

- **Training:** `2023-09-30 00:00` to `< 2024-09-30 00:00` (1 year, ~8784 records with leap year)
- **Testing:** `>= 2024-09-30 00:00` to `< 2025-09-30 00:00` (1 year, ~8760 records)
- **Discard:** All data before `2023-09-30` (not used for training)

**Never use random splits or shuffling** - time series must remain chronological.

## SSA Model Configuration

### Parameter Mapping (Hourly Data)

```csharp
windowSize = 7 * 24;      // 168 hours = 1 week (captures weekly patterns)
seriesLength = 30 * 24;   // 720 hours ≈ 1 month (local context window)
trainSize = 365 * 24;     // 8760 hours = 1 year (full seasonal cycle)
horizon = 24;             // 24 hours ahead (forecast horizon)
confidenceLevel = 0.95f;  // 95% confidence intervals
```

### Constraints
- `windowSize < seriesLength <= trainSize`
- `trainSize <= trainList.Count`
- If constraints violated, reduce `trainSize` or `seriesLength`

### SSA Theory (Brief)
SSA decomposes time series into: **Trend + Seasonality + Noise** via trajectory matrix factorization. The algorithm captures:
- Daily patterns (24h cycle)
- Weekly patterns (168h cycle via windowSize)
- Monthly context (720h via seriesLength)
- Annual seasonality (8760h via trainSize)

## ML.NET Data Models

Located in `Models/` folder with namespace `PowerDemandForecasting.Models`:

```csharp
// Input schema for cleaned CSV (ModelInput.cs)
public class ModelInput
{
    [LoadColumn(0)]
    public DateTime Timestamp { get; set; }

    [LoadColumn(1)]
    public float Stromverbrauch { get; set; }
}

// Output schema for predictions (ModelOutput.cs)
public class ModelOutput
{
    public float[] ForecastedValues { get; set; }
    public float[] LowerBoundValues { get; set; }
    public float[] UpperBoundValues { get; set; }
}
```

## Execution Flow (Program.cs)

**Current implementation (Phases 0-5 complete):**
1. **Setup:** Define paths, create `MLContext(seed: 0)`
2. **Clean:** Run `CleanData(rawPath, cleanPath)` to generate cleaned CSV
3. **Load:** Use `LoadData()` with TextLoader to read `el_power_clean.csv`
4. **Validate:** Run `PerformQualityChecks()` - check for NaN/negatives, log date ranges
5. **DST Fix:** Run `HandleDstDuplicatesAndGaps()` to merge October duplicates and interpolate March gaps
6. **Split:** Run `CreateTrainTestFiles()` to create physical train/test CSV files
7. **Train:** Run `TrainModel()` with dynamic trainSize calculation - `Program.cs:636-685`
8. **Evaluate:** Run `EvaluateAndExport()` to calculate MAE/RMSE and export details - `Program.cs:687-751`

**Pending implementation (Phase 6+):**
9. **Refinement:** Finalize documentation and comments (Phase 6)
10. **Forecast:** Optional future predictions beyond test set (Phase 10)

## Evaluation Metrics

- **MAE (Mean Absolute Error):** Average deviation in MW (lower = better)
- **RMSE (Root Mean Squared Error):** Penalizes large errors (lower = better)
- **Relative Error:** `(MAE or RMSE) / meanLoad * 100%` for context

Output includes both absolute (MW) and relative (%) errors.

## Common Pitfalls (FROM PLAN.md Phase 9)

1. **Wrong column selected:** Use Column 9 (`Stromverbrauch`), NOT Column 1 (empty)
2. **Decimal separator ignored:** Must convert `,` → `.` in `CleanData()`
3. **Time series shuffled:** Never use random splits; always filter by timestamp
4. **Invalid SSA parameters:** Ensure `windowSize < seriesLength <= trainSize <= trainList.Count`
5. **Horizon too large:** Start with `horizon = 24` or `168`; larger = less accurate
6. **Missing values unhandled:** Skip rows with empty `Stromverbrauch` in `CleanData()`
7. **DST not handled:** October duplicates and March gaps MUST be corrected (already implemented in Phase 3)

## NuGet Dependencies

```xml
<PackageReference Include="Microsoft.ML" Version="5.0.0" />
<PackageReference Include="Microsoft.ML.TimeSeries" Version="5.0.0" />
```

Target framework: `.NET 8.0` (SDK version 8.0.121)

## Culture & Parsing

All parsing uses `CultureInfo.InvariantCulture`:
- `DateTime.ParseExact("yyyy-MM-dd HH:mm:ss", InvariantCulture)`
- `float.Parse(value, InvariantCulture)` after replacing `,` with `.`
- Output files use `.` as decimal separator for maximum compatibility

## Output Files

**Auto-generated files (do not edit manually):**
- `Data/el_power_clean.csv` - Cleaned data (2 columns: Timestamp;Stromverbrauch)
- `Data/el_power_clean_dstfixed.csv` - DST-corrected data (no duplicates/gaps)
- `Data/train_data.csv` - Training split (2023-09-30 to 2024-09-30)
- `Data/test_data.csv` - Testing split (2024-09-30 to 2025-09-30)
- `Data/evaluation_details.csv` - Per-timestamp predictions with confidence intervals (Excel-ready)
- `MLModel.zip` - Serialized forecasting model

## Troubleshooting

### Build Issues
- **"The type or namespace name 'ML' does not exist"**: Run `dotnet restore` to ensure NuGet packages are installed
- **Missing Models namespace**: Ensure `ModelInput.cs` and `ModelOutput.cs` are in `Models/` folder with correct namespace

### Data Issues
- **"Cleaned data file not found"**: Ensure `Data/el_dataset_h.csv` exists and run the application (CleanData runs automatically)
- **Unexpected record counts**: Check console output from quality checks - should show ~85,463 raw records
- **DST warnings**: October duplicates and March gaps are expected and automatically corrected

### Runtime Issues
- **OutOfMemoryException**: Reduce `trainSize` or `seriesLength` parameters
- **SSA parameter constraint violations**: Ensure `windowSize < seriesLength <= trainSize <= trainList.Count`

## Reference Tutorial

Microsoft Learn: [Forecast bike rental demand - time series - ML.NET](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/time-series-demand-forecasting)

**Key differences from tutorial:**
- SQL Server → **CSV files**
- Daily data → **Hourly data**
- `TotalRentals` → **Stromverbrauch (MW)**
- `windowSize=7, seriesLength=30, trainSize=365` → **7×24, 30×24, 365×24**
- No DST handling → **Automatic DST duplicate/gap correction**
