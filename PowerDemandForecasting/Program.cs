using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using PowerDemandForecasting.Models;

namespace PowerDemandForecasting
{
    /// <summary>
    /// Stores detailed evaluation results for each forecast origin point
    /// </summary>
    public class RollingOriginResult
    {
        public DateTime OriginTimestamp { get; set; }
        public DateTime[] ForecastTimestamps { get; set; } = Array.Empty<DateTime>();
        public float[] ActualValues { get; set; } = Array.Empty<float>();
        public float[] ForecastedValues { get; set; } = Array.Empty<float>();
        public float[] LowerBounds { get; set; } = Array.Empty<float>();
        public float[] UpperBounds { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// Stores aggregated metrics for each forecast horizon (1h to 24h)
    /// </summary>
    public class HorizonMetrics
    {
        public int Horizon { get; set; }
        public double MAE { get; set; }
        public double RMSE { get; set; }
        public double MAPE { get; set; }
        public int SampleCount { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ML.NET Electricity Demand Forecasting ===\n");

            // Phase 1: Data Cleaning
            string rawDataPath = "Data/el_dataset_h.csv";
            string cleanDataPath = "Data/el_power_clean.csv";

            Console.WriteLine("Phase 1: Cleaning raw data...");
            CleanData(rawDataPath, cleanDataPath);
            Console.WriteLine();

            // Phase 2: Load data and perform quality checks
            Console.WriteLine("Phase 2: Loading data and performing quality checks...");
            var mlContext = new MLContext(seed: 0);

            var dataView = LoadData(mlContext, cleanDataPath);
            PerformQualityChecks(mlContext, dataView);
            Console.WriteLine();

            HandleDstDuplicatesAndGaps(mlContext, dataView, "Data/el_power_clean_dstfixed.csv");
            Console.WriteLine();

            // Phase 3: Create Train/Test Split Files
            CreateTrainTestFiles(mlContext, "Data/el_power_clean_dstfixed.csv");
            Console.WriteLine();

            // Phase 4: Train Model
            string modelPath = "Models/forecast_model.zip";
            TrainModel(mlContext, "Data/train_data.csv", modelPath);
            Console.WriteLine();

            // Phase 5: Evaluate & Export
            EvaluateAndExport(mlContext, "Data/test_data.csv", modelPath, "Data/evaluation_details.csv");
            Console.WriteLine();
        }

        /// <summary>
        /// Cleans the raw energy dataset and produces a 2-column CSV:
        /// Timestamp;Stromverbrauch
        ///
        /// Steps:
        /// - Skips leading metadata lines until a line starts with "2016-"
        /// - Parses semicolon-separated values
        /// - Timestamp: column 0
        /// - Stromverbrauch: column 9 (index 9)
        /// - Converts decimal comma to dot and uses InvariantCulture
        /// - Skips rows with empty/invalid Stromverbrauch
        /// - Logs counts of processed, cleaned, skipped lines
        /// </summary>
        /// <param name="inputPath">Path to the raw CSV (e.g. Data/el_dataset_h.csv)</param>
        /// <param name="outputPath">Path for the cleaned CSV (e.g. Data/el_power_clean.csv)</param>
        static void CleanData(string inputPath, string outputPath)
        {
            if (inputPath == null) throw new ArgumentNullException(nameof(inputPath));
            if (outputPath == null) throw new ArgumentNullException(nameof(outputPath));

            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("Input file not found.", inputPath);
            }

            int rawLineIndex = 0;            // Counts all lines read from input
            int dataLineIndex = 0;           // Counts only lines in the data section (after metadata)
            int writtenLines = 0;            // Number of lines successfully written to output
            int skippedEmptyOrInvalid = 0;   // Lines skipped due to empty/invalid Stromverbrauch
            int skippedBeforeData = 0;       // Metadata/header lines skipped before first timestamp

            // Pattern for start of first data timestamp (you can parameterize if needed)
            const string dataStartPrefix = "2016-";

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)
                                      ?? AppDomain.CurrentDomain.BaseDirectory);

            using (var reader = new StreamReader(inputPath))
            using (var writer = new StreamWriter(outputPath, false)) // overwrite existing
            {
                // Write header to the cleaned file
                writer.WriteLine("Timestamp;Stromverbrauch");

                bool inDataSection = false;

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    rawLineIndex++;

                    // Normalize quotes and whitespace around line
                    line = line.Trim();

                    // Skip blank lines entirely
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    // Detect the first actual data line by prefix (e.g., "2016-..." or ""2016-...")
                    if (!inDataSection)
                    {
                        // Check if line starts with quote + year or just year
                        if (line.StartsWith("\"" + dataStartPrefix, StringComparison.Ordinal) ||
                            line.StartsWith(dataStartPrefix, StringComparison.Ordinal))
                        {
                            inDataSection = true;
                        }
                        else
                        {
                            skippedBeforeData++;
                            continue;
                        }
                    }

                    dataLineIndex++;

                    // Now parse semicolon-separated columns
                    string[] columns = line.Split(';');

                    if (columns.Length <= 9)
                    {
                        // Not enough columns; skip but count as invalid
                        skippedEmptyOrInvalid++;
                        continue;
                    }

                    string timestampRaw = TrimQuotes(columns[0]);
                    string stromverbrauchRaw = TrimQuotes(columns[9]);

                    // Skip rows where consumption is empty or whitespace
                    if (string.IsNullOrWhiteSpace(stromverbrauchRaw))
                    {
                        skippedEmptyOrInvalid++;
                        continue;
                    }

                    // Replace decimal separator: ',' -> '.'
                    stromverbrauchRaw = stromverbrauchRaw.Replace(',', '.');

                    // Validate timestamp format (optional but safer)
                    if (!DateTime.TryParseExact(
                            timestampRaw,
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var parsedTimestamp))
                    {
                        // Could log or count malformed timestamps separately
                        skippedEmptyOrInvalid++;
                        continue;
                    }

                    // Parse consumption as double using InvariantCulture
                    if (!double.TryParse(
                            stromverbrauchRaw,
                            NumberStyles.Float | NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture,
                            out double consumption))
                    {
                        skippedEmptyOrInvalid++;
                        continue;
                    }

                    // Reformat with '.' as decimal separator using InvariantCulture
                    string consumptionFormatted = consumption.ToString(
                        "G", CultureInfo.InvariantCulture);

                    // Write cleaned line
                    // Important: use InvariantCulture string for timestamp too, but keep original format
                    string timestampFormatted = parsedTimestamp.ToString(
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture);

                    writer.WriteLine($"{timestampFormatted};{consumptionFormatted}");
                    writtenLines++;
                }
            }

            // Logging summary
            Console.WriteLine("=== CleanData Summary ===");
            Console.WriteLine($"Input file:  {inputPath}");
            Console.WriteLine($"Output file: {outputPath}");
            Console.WriteLine($"Total lines read (raw):         {rawLineIndex}");
            Console.WriteLine($"Lines skipped before data:      {skippedBeforeData}");
            Console.WriteLine($"Data lines examined:            {dataLineIndex}");
            Console.WriteLine($"Lines written (clean records):  {writtenLines}");
            Console.WriteLine($"Lines skipped (empty/invalid):  {skippedEmptyOrInvalid}");
        }

        /// <summary>
        /// Removes leading/trailing double quotes and surrounding whitespace.
        /// </summary>
        static string TrimQuotes(string input)
        {
            if (input == null) return string.Empty;

            string trimmed = input.Trim();

            if (trimmed.Length >= 2 &&
                trimmed[0] == '"' &&
                trimmed[trimmed.Length - 1] == '"')
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }

            return trimmed.Trim();
        }

        /// <summary>
        /// Loads cleaned CSV data using ML.NET TextLoader.
        /// Configures separator (';'), decimal marker ('.'), and column mapping.
        /// </summary>
        /// <param name="mlContext">ML.NET context</param>
        /// <param name="dataPath">Path to cleaned CSV file</param>
        /// <returns>IDataView containing loaded data</returns>
        static IDataView LoadData(MLContext mlContext, string dataPath)
        {
            if (!File.Exists(dataPath))
            {
                throw new FileNotFoundException("Cleaned data file not found.", dataPath);
            }

            var dataView = mlContext.Data.LoadFromTextFile<ModelInput>(
                path: dataPath,
                separatorChar: ';',
                hasHeader: true,
                allowQuoting: false
            );

            Console.WriteLine($"Data loaded from: {dataPath}");
            return dataView;
        }

        /// <summary>
        /// Performs comprehensive data quality checks:
        /// - Value integrity (NaN, negative, outliers)
        /// - Timestamp integrity (monotonicity, duplicates)
        /// - Basic distribution statistics
        /// - Date range validation for concept drift prevention
        /// </summary>
        /// <param name="mlContext">ML.NET context</param>
        /// <param name="dataView">Data to validate</param>
        static void PerformQualityChecks(MLContext mlContext, IDataView dataView)
        {
            // Convert to in-memory list for detailed checks
            var allRows = mlContext.Data
                .CreateEnumerable<ModelInput>(dataView, reuseRowObject: false)
                .OrderBy(r => r.Timestamp)
                .ToList();

            Console.WriteLine("=== Data Quality Report ===");
            Console.WriteLine($"Total records loaded: {allRows.Count}");

            if (allRows.Count == 0)
            {
                Console.WriteLine("WARNING: No data loaded!");
                return;
            }

            // 1. Timestamp Integrity
            var minTimestamp = allRows.First().Timestamp;
            var maxTimestamp = allRows.Last().Timestamp;
            Console.WriteLine($"Date range: {minTimestamp:yyyy-MM-dd HH:mm} to {maxTimestamp:yyyy-MM-dd HH:mm}");

            int duplicateCount = 0;
            int nonMonotonicCount = 0;
            DateTime? previousTimestamp = null;
            var duplicateTimestamps = new System.Collections.Generic.List<(DateTime timestamp, float value1, float value2)>();

            foreach (var row in allRows)
            {
                if (previousTimestamp.HasValue)
                {
                    if (row.Timestamp == previousTimestamp.Value)
                    {
                        duplicateCount++;
                        // Find the previous row to get its value
                        var prevRow = allRows[allRows.IndexOf(row) - 1];
                        duplicateTimestamps.Add((row.Timestamp, prevRow.Stromverbrauch, row.Stromverbrauch));
                    }
                    else if (row.Timestamp < previousTimestamp.Value)
                    {
                        nonMonotonicCount++;
                    }
                }
                previousTimestamp = row.Timestamp;
            }

            Console.WriteLine($"Duplicate timestamps: {duplicateCount}");
            if (duplicateCount > 0)
            {
                Console.WriteLine("  Duplicate timestamp details:");
                foreach (var dup in duplicateTimestamps)
                {
                    Console.WriteLine($"    {dup.timestamp:yyyy-MM-dd HH:mm:ss} - Values: {dup.value1:F2} MW / {dup.value2:F2} MW");
                }
            }
            Console.WriteLine($"Non-monotonic timestamps: {nonMonotonicCount}");

            // 2. Value Integrity (Power)
            int nanCount = 0;
            int negativeCount = 0;
            int zeroCount = 0;

            foreach (var row in allRows)
            {
                if (float.IsNaN(row.Stromverbrauch) || float.IsInfinity(row.Stromverbrauch))
                {
                    nanCount++;
                }
                else if (row.Stromverbrauch < 0)
                {
                    negativeCount++;
                }
                else if (row.Stromverbrauch == 0)
                {
                    zeroCount++;
                }
            }

            Console.WriteLine($"NaN/Infinity values: {nanCount}");
            Console.WriteLine($"Negative values: {negativeCount}");
            Console.WriteLine($"Zero values: {zeroCount}");

            // 3. Distribution Statistics
            var validValues = allRows
                .Where(r => !float.IsNaN(r.Stromverbrauch) && !float.IsInfinity(r.Stromverbrauch))
                .Select(r => (double)r.Stromverbrauch)
                .ToList();

            if (validValues.Count > 0)
            {
                double min = validValues.Min();
                double max = validValues.Max();
                double mean = validValues.Average();
                double variance = validValues.Average(v => Math.Pow(v - mean, 2));
                double stdDev = Math.Sqrt(variance);

                Console.WriteLine($"\nPower Consumption Statistics (MW):");
                Console.WriteLine($"  Min:     {min:F2}");
                Console.WriteLine($"  Max:     {max:F2}");
                Console.WriteLine($"  Mean:    {mean:F2}");
                Console.WriteLine($"  Std Dev: {stdDev:F2}");
            }

            // 4. Concept Drift Awareness - Check date boundaries
            var trainStart = new DateTime(2023, 9, 30, 0, 0, 0);
            var testStart = new DateTime(2024, 9, 30, 0, 0, 0);

            var beforeTrainCount = allRows.Count(r => r.Timestamp < trainStart);
            var trainCount = allRows.Count(r => r.Timestamp >= trainStart && r.Timestamp < testStart);
            var testCount = allRows.Count(r => r.Timestamp >= testStart);

            Console.WriteLine($"\nTemporal Split Analysis (Concept Drift Prevention):");
            Console.WriteLine($"  Before {trainStart:yyyy-MM-dd} (discarded): {beforeTrainCount}");
            Console.WriteLine($"  Train period ({trainStart:yyyy-MM-dd} to {testStart:yyyy-MM-dd}): {trainCount}");
            Console.WriteLine($"  Test period ({testStart:yyyy-MM-dd} onwards): {testCount}");

            // 5. Quality Summary
            Console.WriteLine("\n=== Quality Check Summary ===");
            bool hasIssues = nanCount > 0 || negativeCount > 0 || duplicateCount > 0 || nonMonotonicCount > 0;

            if (hasIssues)
            {
                Console.WriteLine("WARNING: Data quality issues detected!");
            }
            else
            {
                Console.WriteLine("Data quality: PASSED");
            }

            if (trainCount < 8760)
            {
                Console.WriteLine($"WARNING: Training data has only {trainCount} records (expected ~8760 for 1 year)");
            }

            if (testCount < 1000)
            {
                Console.WriteLine($"WARNING: Test data has only {testCount} records (may be insufficient)");
            }
        }

        static void HandleDstDuplicatesAndGaps(MLContext mlContext, IDataView dataView, string outputPath)
        {
            var allRows = mlContext.Data
                .CreateEnumerable<ModelInput>(dataView, reuseRowObject: false)
                .OrderBy(r => r.Timestamp)
                .ToList();

            var fixedRows = new System.Collections.Generic.List<ModelInput>();
            var octoberDuplicates = new System.Collections.Generic.List<(DateTime Timestamp, float Value1, float Value2, float Mean)>();

            var grouped = allRows
                .GroupBy(r => r.Timestamp)
                .OrderBy(g => g.Key);

            foreach (var g in grouped)
            {
                if (g.Count() == 2 && g.Key.Month == 10)
                {
                    var first = g.First();
                    var second = g.Skip(1).First();
                    float mean = (first.Stromverbrauch + second.Stromverbrauch) / 2f;

                    octoberDuplicates.Add((g.Key, first.Stromverbrauch, second.Stromverbrauch, mean));

                    fixedRows.Add(new ModelInput
                    {
                        Timestamp = g.Key,
                        Stromverbrauch = mean
                    });
                }
                else
                {
                    foreach (var row in g)
                    {
                        fixedRows.Add(row);
                    }
                }
            }

            // Sort fixedRows before gap interpolation
            fixedRows = fixedRows.OrderBy(r => r.Timestamp).ToList();

            Console.WriteLine("=== DST Analysis ===");
            Console.WriteLine($"Found {octoberDuplicates.Count} duplicate hours in October:");
            foreach (var dup in octoberDuplicates)
            {
                Console.WriteLine($"{dup.Timestamp:yyyy-MM-dd HH:mm:ss} - {dup.Value1:F2} MW -> {dup.Value2:F2} MW, Mean = {dup.Mean:F2} MW");
            }

            var missingHours = new System.Collections.Generic.List<DateTime>();

            for (int i = 0; i < fixedRows.Count - 1; i++)
            {
                var current = fixedRows[i].Timestamp;
                var next = fixedRows[i + 1].Timestamp;
                var diff = next - current;

                if (diff.TotalHours > 1.0)
                {
                    for (var t = current.AddHours(1); t < next; t = t.AddHours(1))
                    {
                        missingHours.Add(t);
                    }
                }
            }

            var missingMarch = missingHours
                .Where(t => t.Month == 3)
                .OrderBy(t => t)
                .ToList();

            Console.WriteLine();
            Console.WriteLine($"Missing hourly timestamps in March: {missingMarch.Count}");
            foreach (var ts in missingMarch)
            {
                Console.WriteLine($"Missing: {ts:yyyy-MM-dd HH:mm:ss}");
            }

            // Interpolate March gaps with linear method
            var interpolatedMarch = new System.Collections.Generic.List<(DateTime Timestamp, float Value)>();

            foreach (var missingTs in missingMarch)
            {
                // Find surrounding values
                var prevHour = missingTs.AddHours(-1);
                var nextHour = missingTs.AddHours(1);

                var prevRow = fixedRows.FirstOrDefault(r => r.Timestamp == prevHour);
                var nextRow = fixedRows.FirstOrDefault(r => r.Timestamp == nextHour);

                if (prevRow != null && nextRow != null &&
                    !float.IsNaN(prevRow.Stromverbrauch) &&
                    !float.IsNaN(nextRow.Stromverbrauch))
                {
                    float interpolated = (prevRow.Stromverbrauch + nextRow.Stromverbrauch) / 2f;

                    fixedRows.Add(new ModelInput
                    {
                        Timestamp = missingTs,
                        Stromverbrauch = interpolated
                    });

                    interpolatedMarch.Add((missingTs, interpolated));

                    Console.WriteLine($"Interpolating: {missingTs:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"  Previous ({prevHour:HH:mm}): {prevRow.Stromverbrauch:F2} MW");
                    Console.WriteLine($"  Next ({nextHour:HH:mm}):     {nextRow.Stromverbrauch:F2} MW");
                    Console.WriteLine($"  Interpolated:     {interpolated:F2} MW");
                }
                else
                {
                    Console.WriteLine($"WARNING: Cannot interpolate {missingTs:yyyy-MM-dd HH:mm:ss} (missing surrounding values)");
                }
            }

            // Re-sort after adding interpolated values
            fixedRows = fixedRows.OrderBy(r => r.Timestamp).ToList();

            Console.WriteLine();
            Console.WriteLine($"March gaps interpolated: {interpolatedMarch.Count}");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)
                                      ?? AppDomain.CurrentDomain.BaseDirectory);

            using (var writer = new StreamWriter(outputPath, false))
            {
                writer.WriteLine("Timestamp;Stromverbrauch");
                foreach (var row in fixedRows.OrderBy(r => r.Timestamp))
                {
                    writer.WriteLine($"{row.Timestamp:yyyy-MM-dd HH:mm:ss};{row.Stromverbrauch.ToString(CultureInfo.InvariantCulture)}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"DST-fixed data written to: {outputPath}");
            Console.WriteLine($"Original row count: {allRows.Count}");
            Console.WriteLine($"October duplicates merged: {octoberDuplicates.Count}");
            Console.WriteLine($"March gaps interpolated: {interpolatedMarch.Count}");
            Console.WriteLine($"Final row count: {fixedRows.Count}");

            // --- Final Validation Check ---
            Console.WriteLine("\n=== Final Data Integrity Check ===");
            int finalGaps = 0;
            int finalDups = 0;
            for (int i = 0; i < fixedRows.Count - 1; i++)
            {
                var current = fixedRows[i];
                var next = fixedRows[i + 1];
                var diff = next.Timestamp - current.Timestamp;

                if (diff.TotalHours > 1.01) // Allow slight float tolerance, though DateTime subtraction is precise
                {
                    Console.WriteLine($"FAILURE: Gap detected between {current.Timestamp} and {next.Timestamp}");
                    finalGaps++;
                }
                else if (diff.TotalHours < 0.99) // Duplicate or unordered
                {
                    Console.WriteLine($"FAILURE: Duplicate/Overlap detected between {current.Timestamp} and {next.Timestamp}");
                    finalDups++;
                }
            }

            if (finalGaps == 0 && finalDups == 0)
            {
                Console.WriteLine("SUCCESS: No gaps or duplicates found in the final dataset.");
            }
            else
            {
                Console.WriteLine($"WARNING: Final check failed! Found {finalGaps} gaps and {finalDups} duplicates.");
            }
        }
        static void CreateTrainTestFiles(MLContext mlContext, string inputPath)
        {
            Console.WriteLine("Phase 3: Creating Train/Test Split Files...");

            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("DST-fixed data file not found.", inputPath);
            }

            // Load the fully cleaned and fixed data
            var dataView = mlContext.Data.LoadFromTextFile<ModelInput>(
                path: inputPath,
                separatorChar: ';',
                hasHeader: true,
                allowQuoting: false
            );

            var allRows = mlContext.Data
                .CreateEnumerable<ModelInput>(dataView, reuseRowObject: false)
                .OrderBy(r => r.Timestamp)
                .ToList();

            // Define strict time ranges
            // Train: 2023-09-30 00:00 (inclusive) to 2024-09-30 00:00 (exclusive)
            // Test:  2024-09-30 00:00 (inclusive) to 2025-09-30 00:00 (exclusive)
            var trainStart = new DateTime(2023, 9, 30, 0, 0, 0);
            var testStart = new DateTime(2024, 9, 30, 0, 0, 0);
            var testEnd = new DateTime(2025, 9, 30, 0, 0, 0);

            var trainData = allRows
                .Where(r => r.Timestamp >= trainStart && r.Timestamp < testStart)
                .ToList();

            var testData = allRows
                .Where(r => r.Timestamp >= testStart && r.Timestamp < testEnd)
                .ToList();

            string trainPath = "Data/train_data.csv";
            string testPath = "Data/test_data.csv";

            // Local helper to write CSV
            void WriteCsv(string path, System.Collections.Generic.List<ModelInput> rows)
            {
                using (var writer = new StreamWriter(path, false))
                {
                    writer.WriteLine("Timestamp;Stromverbrauch");
                    foreach (var row in rows)
                    {
                        writer.WriteLine($"{row.Timestamp:yyyy-MM-dd HH:mm:ss};{row.Stromverbrauch.ToString(CultureInfo.InvariantCulture)}");
                    }
                }
            }

            WriteCsv(trainPath, trainData);
            WriteCsv(testPath, testData);

            Console.WriteLine($"Train data written to: {trainPath}");
            Console.WriteLine($"  Range: {trainStart:yyyy-MM-dd HH:mm} to < {testStart:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  Count: {trainData.Count} records");
            
            // Validation warning for Train size
            if (trainData.Count < 8760) Console.WriteLine("  WARNING: Train data seems too small (< 8760)!");

            Console.WriteLine($"Test data written to: {testPath}");
            Console.WriteLine($"  Range: {testStart:yyyy-MM-dd HH:mm} to < {testEnd:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  Count: {testData.Count} records");
            
            // Validation warning for Test size
            if (testData.Count == 0) Console.WriteLine("  WARNING: Test data is empty!");
        }

        static void TrainModel(MLContext mlContext, string trainDataPath, string modelPath)
        {
            Console.WriteLine("Phase 4: Training the SSA Forecasting Model...");

            if (!File.Exists(trainDataPath))
            {
                throw new FileNotFoundException("Train data not found", trainDataPath);
            }

            // Load the training data
            var dataView = mlContext.Data.LoadFromTextFile<ModelInput>(
                path: trainDataPath,
                separatorChar: ';',
                hasHeader: true,
                allowQuoting: false
            );

            // Dynamic trainSize calculation
            // We count the actual rows in the training set (should be 8784 for the leap year 2023-2024)
            var rowCount = mlContext.Data.CreateEnumerable<ModelInput>(dataView, reuseRowObject: false).Count();
            int trainSize = rowCount;

            Console.WriteLine($"Dynamic trainSize determined: {trainSize} (Use this for SSA training)");

            // SSA Forecast Pipeline
            // Algorithm: Singular Spectrum Analysis (SSA)
            // Decomposes the time-series into Trend, Seasonality, and Noise components.
            // Reference: https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/time-series-demand-forecasting
            var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "ForecastedValues",
                inputColumnName: "Stromverbrauch",
                windowSize: 7 * 24,      // 168h - Weekly seasonality (7 days * 24h)
                seriesLength: 30 * 24,   // 720h - Monthly context (30 days * 24h)
                trainSize: trainSize,    // Dynamic: 8784 or 8760 depending on data
                horizon: 24,             // 24h forecast
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: "LowerBoundValues",
                confidenceUpperBoundColumn: "UpperBoundValues"
            );

            // Train the model
            Console.WriteLine("Fitting the model...");
            var model = forecastingPipeline.Fit(dataView);

            // Save the model
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath) ?? "Models");
            mlContext.Model.Save(model, dataView.Schema, modelPath);

            Console.WriteLine($"Model saved to: {modelPath}");
        }

        static void EvaluateAndExport(MLContext mlContext, string testDataPath, string modelPath, string exportPath)
        {
            Console.WriteLine("Phase 5: Rolling-Origin Multi-Step Evaluation...");

            if (!File.Exists(testDataPath)) throw new FileNotFoundException("Test data not found", testDataPath);
            if (!File.Exists(modelPath)) throw new FileNotFoundException("Model file not found", modelPath);

            // Load Test Data
            var testDataView = mlContext.Data.LoadFromTextFile<ModelInput>(
                path: testDataPath,
                separatorChar: ';',
                hasHeader: true,
                allowQuoting: false
            );

            var testRows = mlContext.Data
                .CreateEnumerable<ModelInput>(testDataView, reuseRowObject: false)
                .OrderBy(r => r.Timestamp)
                .ToList();

            Console.WriteLine($"Test data loaded: {testRows.Count} records");

            // Load Model
            ITransformer model;
            using (var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                model = mlContext.Model.Load(stream, out var _);
            }

            // STEP 3: Initialize TimeSeriesPredictionEngine
            var forecastEngine = model.CreateTimeSeriesEngine<ModelInput, ModelOutput>(mlContext);
            Console.WriteLine("TimeSeriesPredictionEngine initialized.");

            // STEP 4: Rolling-Origin Evaluation Loop
            const int HORIZON = 24;
            var rollingResults = new System.Collections.Generic.List<RollingOriginResult>();
            
            // We need at least HORIZON+1 points for proper evaluation
            int maxOrigins = testRows.Count - HORIZON;
            Console.WriteLine($"Performing rolling-origin evaluation with {maxOrigins} origins...\n");

            for (int originIdx = 0; originIdx < maxOrigins; originIdx++)
            {
                var originPoint = testRows[originIdx];
                
                // Display progress every 100 origins
                if (originIdx % 100 == 0)
                {
                    Console.WriteLine($"Progress: Origin {originIdx + 1}/{maxOrigins} - {originPoint.Timestamp:yyyy-MM-dd HH:mm}");
                }

                // Make 24-hour forecast from this origin
                var forecast = forecastEngine.Predict();

                // Collect actual values for the next 24 hours
                var actualValues = new float[HORIZON];
                var forecastTimestamps = new DateTime[HORIZON];
                
                for (int h = 0; h < HORIZON; h++)
                {
                    int targetIdx = originIdx + 1 + h; // +1 because we forecast from after the origin
                    if (targetIdx < testRows.Count)
                    {
                        actualValues[h] = testRows[targetIdx].Stromverbrauch;
                        forecastTimestamps[h] = testRows[targetIdx].Timestamp;
                    }
                }

                // Store results
                rollingResults.Add(new RollingOriginResult
                {
                    OriginTimestamp = originPoint.Timestamp,
                    ForecastTimestamps = forecastTimestamps,
                    ActualValues = actualValues,
                    ForecastedValues = forecast.ForecastedValues,
                    LowerBounds = forecast.LowerBoundValues,
                    UpperBounds = forecast.UpperBoundValues
                });

                // Update the engine state with the actual observed value
                forecastEngine.CheckPoint(mlContext, modelPath);
                forecastEngine.Predict(originPoint);
            }

            Console.WriteLine($"\nRolling-origin evaluation complete: {rollingResults.Count} forecasts generated.\n");

            // STEP 5: Calculate Per-Horizon Metrics
            var horizonMetrics = CalculateHorizonMetrics(rollingResults, HORIZON);

            // Display metrics
            Console.WriteLine("=== Per-Horizon Evaluation Metrics ===");
            Console.WriteLine($"{"Horizon",-10} {"MAE (MW)",-15} {"RMSE (MW)",-15} {"MAPE (%)",-15} {"Samples",-10}");
            Console.WriteLine(new string('-', 75));

            foreach (var hm in horizonMetrics)
            {
                Console.WriteLine($"{hm.Horizon + "h",-10} {hm.MAE,-15:F3} {hm.RMSE,-15:F3} {hm.MAPE,-15:F2} {hm.SampleCount,-10}");
            }

            // Overall metrics
            double overallMAE = horizonMetrics.Average(h => h.MAE);
            double overallRMSE = horizonMetrics.Average(h => h.RMSE);
            double overallMAPE = horizonMetrics.Average(h => h.MAPE);

            Console.WriteLine(new string('-', 75));
            Console.WriteLine($"{"Overall",-10} {overallMAE,-15:F3} {overallRMSE,-15:F3} {overallMAPE,-15:F2}");
            Console.WriteLine();

            // Calculate Confidence Interval Coverage
            Console.WriteLine("\n=== Confidence Interval Coverage Analysis ===");
            int inBoundsCount = 0;
            int totalCount = 0;
            var coverageByHorizon = new double[HORIZON];
            var countsPerHorizon = new int[HORIZON];

            foreach (var result in rollingResults)
            {
                for (int h = 0; h < HORIZON; h++)
                {
                    if (h < result.ActualValues.Length)
                    {
                        float actual = result.ActualValues[h];
                        float lower = result.LowerBounds[h];
                        float upper = result.UpperBounds[h];
                        
                        if (!float.IsNaN(actual) && !float.IsNaN(lower) && !float.IsNaN(upper))
                        {
                            totalCount++;
                            countsPerHorizon[h]++;
                            
                            if (actual >= lower && actual <= upper)
                            {
                                inBoundsCount++;
                                coverageByHorizon[h]++;
                            }
                        }
                    }
                }
            }

            double overallCoverage = (double)inBoundsCount / totalCount * 100.0;
            Console.WriteLine($"Overall 95% CI Coverage: {overallCoverage:F2}% (Expected: ~95%)");

            if (Math.Abs(overallCoverage - 95.0) > 5.0)
            {
                Console.WriteLine("WARNING: Coverage deviates significantly from expected 95%!");
            }

            // Per-horizon coverage
            Console.WriteLine("\nCoverage by Horizon:");
            for (int h = 0; h < HORIZON; h += 6) // Show every 6 hours
            {
                if (countsPerHorizon[h] > 0)
                {
                    double coverage = (coverageByHorizon[h] / countsPerHorizon[h]) * 100.0;
                    Console.WriteLine($"  H{h+1:D2}: {coverage:F2}%");
                }
            }
            Console.WriteLine();

            // STEP 6: Export Extended CSV
            ExportDetailedResults(rollingResults, horizonMetrics, exportPath);
            Console.WriteLine($"Detailed evaluation results exported to: {exportPath}");
        }

        // STEP 7: Helper method for per-horizon metrics calculation
        static System.Collections.Generic.List<HorizonMetrics> CalculateHorizonMetrics(
            System.Collections.Generic.List<RollingOriginResult> results, int horizon)
        {
            var metrics = new System.Collections.Generic.List<HorizonMetrics>();

            for (int h = 0; h < horizon; h++)
            {
                var errors = new System.Collections.Generic.List<double>();
                var mapeErrors = new System.Collections.Generic.List<double>();

                foreach (var result in results)
                {
                    if (h < result.ActualValues.Length && h < result.ForecastedValues.Length)
                    {
                        float actual = result.ActualValues[h];
                        float forecast = result.ForecastedValues[h];

                        // Skip invalid values
                        if (float.IsNaN(actual) || float.IsNaN(forecast) ||
                            float.IsInfinity(actual) || float.IsInfinity(forecast))
                            continue;

                        double error = actual - forecast;
                        errors.Add(error);

                        // MAPE calculation (avoid division by zero)
                        if (Math.Abs(actual) > 0.01)
                        {
                            mapeErrors.Add(Math.Abs(error / actual) * 100.0);
                        }
                    }
                }

                if (errors.Count > 0)
                {
                    double mae = errors.Average(e => Math.Abs(e));
                    double rmse = Math.Sqrt(errors.Average(e => e * e));
                    double mape = mapeErrors.Count > 0 ? mapeErrors.Average() : 0.0;

                    metrics.Add(new HorizonMetrics
                    {
                        Horizon = h + 1,
                        MAE = mae,
                        RMSE = rmse,
                        MAPE = mape,
                        SampleCount = errors.Count
                    });
                }
            }

            return metrics;
        }

        // STEP 8: Helper method for detailed CSV export
        static void ExportDetailedResults(
            System.Collections.Generic.List<RollingOriginResult> results,
            System.Collections.Generic.List<HorizonMetrics> metrics,
            string exportPath)
        {
            // Create directory if needed
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath)
                                      ?? AppDomain.CurrentDomain.BaseDirectory);

            using (var writer = new StreamWriter(exportPath, false))
            {
                // Write header with all horizons
                writer.Write("OriginTimestamp");
                for (int h = 1; h <= 24; h++)
                {
                    writer.Write($";Actual_H{h};Forecast_H{h};Error_H{h};LowerBound_H{h};UpperBound_H{h}");
                }
                writer.WriteLine();

                // Write data rows
                foreach (var result in results)
                {
                    writer.Write($"{result.OriginTimestamp:yyyy-MM-dd HH:mm:ss}");

                    for (int h = 0; h < 24; h++)
                    {
                        if (h < result.ActualValues.Length)
                        {
                            float actual = result.ActualValues[h];
                            float forecast = result.ForecastedValues[h];
                            float error = actual - forecast;
                            float lower = result.LowerBounds[h];
                            float upper = result.UpperBounds[h];

                            // Clamp negative bounds to 0
                            if (lower < 0) lower = 0;

                            writer.Write($";{actual:F3};{forecast:F3};{error:F3};{lower:F3};{upper:F3}");
                        }
                        else
                        {
                            writer.Write(";;;;;"); // Empty values if not available
                        }
                    }
                    writer.WriteLine();
                }

                // Append summary statistics section
                writer.WriteLine();
                writer.WriteLine("=== Per-Horizon Summary Statistics ===");
                writer.WriteLine("Horizon;MAE_MW;RMSE_MW;MAPE_Percent;SampleCount");
                
                foreach (var hm in metrics)
                {
                    writer.WriteLine($"{hm.Horizon};{hm.MAE:F3};{hm.RMSE:F3};{hm.MAPE:F2};{hm.SampleCount}");
                }
            }
        }
    }
}
