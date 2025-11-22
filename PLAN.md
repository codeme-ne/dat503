## PROMPT FÜR CODEX HIGH THINKING

(Stromverbrauchs‑Forecast Österreich mit ML.NET / SSA)

---

## Fortschritt (Stand: 2025-11-22)

- **Phase 0 – Projektgrundlage**: **ERLEDIGT**
  - .NET 8 Konsolenprojekt `PowerDemandForecasting` erstellt, Build erfolgreich.
  - Ordnerstruktur angelegt: `Data/`, `Models/`.
  - NuGet-Pakete installiert: `Microsoft.ML` 5.0.0, `Microsoft.ML.TimeSeries` 5.0.0.
  - Rohdatei `Data/el_dataset_h.csv` ins Projekt eingebunden.

- **Phase 1 – Datenbereinigung**: **ERLEDIGT**
  - `CleanData()` implementiert (synchron, robuste Fehlerbehandlung + Logging).
  - Metadaten: 14 Kopfzeilen korrekt erkannt und übersprungen.
  - Verarbeitete Datenzeilen: **85.463 gültige Records**.
  - Zeitabdeckung der bereinigten Daten: **2016–2025 (stündlich)**.
  - Ausgabe: `Data/el_power_clean.csv` mit exakt 2 Spalten:
    - `Timestamp` (Format `yyyy-MM-dd HH:mm:ss`, `InvariantCulture`)
    - `Stromverbrauch` (Float mit Dezimalpunkt, `InvariantCulture`)

- **Phase 2 – Data Models & Loading**: **ERLEDIGT**
  - DTOs (`ModelInput`, `ModelOutput`) erstellt.
  - `LoadData` & `PerformQualityChecks` implementiert (DST-Korrektur, Lückenfüllung).
  - Validierte Daten: `Data/el_power_clean_dstfixed.csv`.

- **Phase 3 – Train/Test Split**: **ERLEDIGT**
  - Temporaler Split implementiert (Concept Drift Prevention).
  - Physische Dateien erstellt:
    - `Data/train_data.csv` (30.09.2023 - 29.09.2024, 8784 Records).
    - `Data/test_data.csv` (30.09.2024 - 29.09.2025, 8760 Records).

- **Phase 4–10 (ML-Pipeline, Evaluation, Forecasting, Doku)**: **AUSSTEHEND**
  - Nächste Schritte: SSA-Pipeline konfigurieren, Training, Evaluation (MAE/RMSE), CSV-Export, Zukunfts-Forecast.

- **Phase 5 – Evaluation & Export**: **ERLEDIGT**
  - Modelltraining und Evaluation erfolgreich durchgeführt.
  - Metriken: MAE = 261.16 MW (3.92%), RMSE = 339.40 MW (5.09%).
  - Ergebnisse exportiert nach `Data/evaluation_details.csv`.
  - Modell gespeichert unter `Models/forecast_model.zip`.

- **Phase 6 – Integration & Documentation**: **IN ARBEIT**
  - Nächste Schritte: Code-Kommentare verfeinern, Projektdokumentation in AGENTS.md finalisieren.

---

## Ausführungsplan (Generiert von Zen MCP Planner)

**Übersicht:** 14 Tasks in 6 Batches, sequentielle Abhängigkeiten, Checkpoints nach jedem Batch.

```text
EXECUTION FLOW (Sequential Dependencies)

┌──────────────────────────────────────────────────────────┐
│ BATCH 1: Foundation & Setup                             │
│  1.1 → 1.2 → 1.3                                        │
│  (Project + Folders + CleanData)                        │ ✓ ERLEDIGT
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────┐
│ BATCH 2: Data Models & Loading                          │
│  2.1 → 2.2 → 2.3                                        │
│  (DTOs + TextLoader + Quality Checks)                   │ ✓ ERLEDIGT
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────┐
│ BATCH 3: Train/Test Split                               │
│  3.1                                                     │
│  (Temporal Split: 2023-2024 train, 2024+ test)         │ ✓ ERLEDIGT
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────┐
│ BATCH 4: SSA Pipeline & Training                        │
│  4.1 → 4.2                                              │
│  (Configure Parameters + Train Model)                   │ ✅ ERLEDIGT
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────┐
│ BATCH 5: Evaluation & Export                            │
│  5.1 → 5.2 → 5.3                                        │
│  (Metrics + CSV Export + Save Model)                    │ ✓ ERLEDIGT
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────┐
│ BATCH 6: Integration & Documentation                    │
│  6.1 → 6.2                                              │
│  (Assemble Program.cs + Add Comments)                   │ ⏳ IN ARBEIT
└──────────────────────────────────────────────────────────┘
```

### BATCH 1: Foundation & Setup ✓ ERLEDIGT

**Task 1.1:** .NET 8 Console Project

- Command: `dotnet new console -n PowerDemandForecasting`
- Verify: `dotnet build` succeeds ✓

**Task 1.2:** Setup Structure & Dependencies

- Folders: `Data/`, `Models/` ✓
- NuGet: `Microsoft.ML`, `Microsoft.ML.TimeSeries` ✓
- Verify: Packages in .csproj ✓

**Task 1.3:** CleanData() Method

- Parse `el_da**Status:** ✅ Abgeschlossen
- Implementiert in `HandleDstDuplicatesAndGaps`
- Validierung: 10 März-Lücken interpoliert, 9 Oktober-Duplikate gemittelt.
- Final Integrity Check: 0 Gaps, 0 Duplicates. Datei: `el_power_clean_dstfixed.csv`.

**Wichtige Implementierungsdetails (Essentiell für Dokumentation):**
1. **Dezimaltrennzeichen:** Die Ausgabe in die CSV-Datei erfolgt strikt mit **Punkt (.)** als Dezimaltrennzeichen (`CultureInfo.InvariantCulture`). Dies verhindert Parsing-Fehler in nachfolgenden ML-Schritten, unabhängig von der lokalen Systemeinstellung (z.B. de-DE Komma).
2. **Datenfluss (Non-Destructive):** Die Korrektur überschreibt nicht die Eingabedatei.
   - Input: `el_power_clean.csv` (Bereinigt, aber mit Zeit-Lücken/Duplikaten)
   - Output: `el_power_clean_dstfixed.csv` (Lückenlos, eindeutig, validiert)
   Dies sichert die Nachvollziehbarkeit jeder Transformationsstufe.

---

### BATCH 2: Data Models & Loading ✓ ERLEDIGT

**Task 2.1:** Define DTOs

- `ModelInput { DateTime Timestamp, float Stromverbrauch }`
- `ModelOutput { float[] ForecastedValues, LowerBoundValues, UpperBoundValues }`
- Verify: Classes compile

**Task 2.2:** Configure TextLoader

- Separator: `;`, Decimal: `.`
- Load `el_power_clean.csv` → IDataView
- Verify: No load errors

**Task 2.3:** Data Quality Checks

- Convert to `List<ModelInput>`, sort by Timestamp
- Log: count, min/max dates
- Check: NaN, negative values
- Verify: Quality report prints

**Success Criteria:**

- Data loads without exceptions
- Date range: 2016-2025
- No NaN/negative values

---

### BATCH 3: Train/Test Split ✅ ABGESCHLOSSEN

**Task 3.1:** Temporal Split (Concept Drift Prevention)

- Train: `2023-09-30 00:00` bis `< 2024-09-30 00:00`
- Test: `>= 2024-09-30 00:00` bis `< 2025-09-30 00:00`
- Create: `Data/train_data.csv` & `Data/test_data.csv` (Physische Dateien für bessere Transparenz)
- Verify: Split sizes reasonable

**Success Criteria:**

- trainList: 8784 records (Schaltjahr 2024) ✅
- testList: 8760 records (1 Jahr) ✅
- No overlap ✅
- Dateien erfolgreich erstellt ✅

---

### BATCH 4: SSA Pipeline & Training ✅ ERLEDIGT

**Task 4.1:** Configure SSA Parameters

- `windowSize = 168` (1 week = 7 × 24h)
- `seriesLength = 720` (1 month ≈ 30 × 24h)
- `trainSize` = Dynamisch berechnet (8784 für Schaltjahr, 8760 für Normaljahr)
- `horizon = 24` (24 hours ahead)
- `confidence = 0.95`
- Verify: `windowSize < seriesLength <= trainSize <= trainList.Count` ✅

**Task 4.2:** Train SSA Model

- Build: `ForecastBySsa` pipeline
- Fit: on `trainData`
- Verify: Training completes
- Model saved to: `Models/forecast_model.zip`
- **Implementierungs-Detail:** `trainSize` wird zur Laufzeit basierend auf `trainData.Count()` gesetzt.

**Success Criteria:**

- No parameter constraint violations ✅
- Training completes in < 5 minutes ✅

---

### BATCH 5: Evaluation & Export ✅ ERLEDIGT
   
**Task 5.1:** Evaluate Model

- Transform `testData`
- Extract: actual vs forecasted values
- Calculate: MAE, RMSE, relative errors
- Verify: Metrics print to console

**Task 5.2:** Export Evaluation Details

- CSV columns: `Timestamp;Actual_Consumption;Forecast_Value;Lower_Bound;Upper_Bound`
- Save to: `Data/evaluation_details.csv`
- Verify: Excel-readable

**Task 5.3:** Save Model & Future Forecast

- Create: `TimeSeriesPredictionEngine` (Partially done: Model saved)
- Checkpoint: `Models/forecast_model.zip`
- Implement: `ForecastFuture()` console output (Next Step)
- Verify: Model saved

**Success Criteria:**

- MAE/RMSE < 20% relative error (baseline) ✅ (Actual: ~3.9%)
- `evaluation_details.csv` opens in Excel ✅
- `forecast_model.zip` created ✅

**Results:**
- MAE: 261.16 MW
- RMSE: 339.40 MW

---

### BATCH 6: Integration & Documentation ⏳ IN ARBEIT

**Task 6.1:** Assemble Complete Program.cs

- Integrate all methods in order
- Add error prevention checks (Phase 9)
- Verify: Full end-to-end run succeeds

**Task 6.2:** Add Documentation

- Comment SSA parameter reasoning
- Reference bike tutorial
- Verify: Code readable

**Success Criteria:**

- Full end-to-end run completes
- Output matches PLAN.md examples

---

### KRITISCHE FEHLER-VERMEIDUNG (Checklist)

Vor jedem Batch validieren:

- ✓ Spalte 9 (`Stromverbrauch`) verwenden, NICHT Spalte 1 (leer)
- ✓ `,` → `.` in CleanData() konvertieren
- ✓ Keine zufälligen Splits (nur chronologisch)
- ✓ Leere Stromverbrauch-Zeilen überspringen
- `DecimalMarker = '.'` in TextLoader setzen
- SSA-Constraints vor `Fit()` validieren
- `CultureInfo.InvariantCulture` für alle Parsings verwenden

---

**Rolle:**
Du bist ein Senior‑C#‑Entwickler und ML.NET‑Experte. Du arbeitest in einem High‑Thinking‑Run mit Auto‑Approve und sollst in einem Rutsch eine saubere, nachvollziehbare Lösung bauen.

**Ziel:**
Baue eine .NET‑8‑Konsolenanwendung, die den **stündlichen Stromverbrauch** in Österreich (MW, Spalte „Stromverbrauch") als **univariate Zeitreihe** mit **SSA (Singular Spectrum Analysis)** in ML.NET prognostiziert.
Die Architektur soll sich am offiziellen Bike‑Sharing‑Tutorial („Forecast bike rental demand“) orientieren, aber auf:

* CSV statt SQL,
* stündliche Werte statt tägliche,
* Stromverbrauch statt Fahrradmieten

angepasst werden.

Die Dateien, die du als Kontext annehmen kannst:

* `Data/el_dataset_h.csv` – Rohdaten (stündlicher Stromverbrauch Österreich, inclusive Metadaten).
* `Tutorial_Vorhersage_des_Bedarfs_für_Fahrradvermietungen_–_Zeitreihe_-_ML.NET.pdf` – offizielles ML.NET‑Tutorial.
* `Forecasting_BikeSharingDemand`‑Sample (README + Program.cs).
* `SSA_encyclopedia.pdf` – kurzer Überblick zu SSA (Trend/Saison/Rauschen, Trajektorienmatrix).

Arbeite strikt schrittweise gemäß folgendem Plan.

---

## Phase 0 – Projektgrundlage

1. **Neues Projekt anlegen**

   * .NET 8 Konsolenanwendung (verwendet: SDK 8.0.121), z. B. `PowerDemandForecasting`.
   * Ordnerstruktur:

     * `Data/` – für CSV‑Dateien.
     * `Models/` – optional für DTO‑Klassen.

2. **NuGet‑Pakete installieren**

   * `Microsoft.ML`
   * `Microsoft.ML.TimeSeries` ([Microsoft Learn][1])

3. **Pfade und MLContext**

   * In `Program.cs`:

     * `rootDir`, `dataDir`, Pfad zur Rohdatei `el_dataset_h.csv`.
     * Später: Pfad zur bereinigten Datei `el_power_clean.csv` und Modellpfad `MLModel.zip`.
   * `var mlContext = new MLContext(seed: 0);`

---

## Phase 1 – Rohdaten aus `el_dataset_h.csv` programmatisch bereinigen

### 1.1 Struktur der Rohdatei verstehen

Inhalt (vereinfacht):

* Erste ~14 Zeilen: Metadaten, inkl. Kopfzeilen wie
  `"Header & Timestamp";"1";"2";...`
  `"KOMP";"Inlandstromverbrauch";"Exporte";...;"Stromverbrauch";...`
* Ab Zeile 15: stündliche Daten:

  ```text
  "2016-01-01 00:00:00";;"1830,877";"955,164";...;"6104,931";...
  ```

Eigenschaften:

* Trennzeichen: `;`
* Dezimaltrennzeichen: `,`
* Spalten:

  * Spalte 0: Timestamp (inkl. Anführungszeichen).
  * Spalte 1: „Inlandstromverbrauch“ – in den Datenzeilen **leer** (`""` → `;;`).
  * Spalte 9: „Stromverbrauch“ – Zielvariable (z. B. `6104,931`).


     ```text
     Timestamp;Stromverbrauch
     ```

   * Danach für jede gültige Zeile:

     ```text
     2016-01-01 00:00:00;6104.931
     2016-01-01 01:00:00;...
     ```

Damit vermeidest du Komma‑Probleme und verschlankst die Datei auf genau 2 Spalten.

Rufe `CleanData(...)` gleich zu Beginn von `Main` auf; bei existierender Ziel‑Datei kannst du optional einen einfachen Check einbauen (z. B. nur neu erzeugen, wenn sie fehlt).

---

## Phase 2 – ML.NET‑Datenmodell & Laden der bereinigten CSV

### 2.1 ModelInput / ModelOutput

**ModelInput** (für die bereinigte Datei):

```csharp
public class ModelInput
{
    [LoadColumn(0)]
    public DateTime Timestamp { get; set; }

    [LoadColumn(1)]
    public float Stromverbrauch { get; set; }
}
```

**ModelOutput** (analog Bike‑Sample, aber generischer Name):

```csharp
public class ModelOutput
{
    public float[] ForecastedValues { get; set; }
    public float[] LowerBoundValues { get; set; }
    public float[] UpperBoundValues { get; set; }
}
```

### 2.2 TextLoader konfigurieren

Nutze `TextLoader` für `el_power_clean.csv`:

```csharp
var textLoaderOptions = new TextLoader.Options
{
    Separators = new[] { ';' },
    HasHeader = true,
    AllowQuoting = false,
    DecimalMarker = '.' // CleanData hat Komma bereits ersetzt
};

var loader   = mlContext.Data.CreateTextLoader<ModelInput>(textLoaderOptions);
var allData  = loader.Load(cleanPath);
```

---

## Phase 3 – Datenqualität prüfen & in Speicher holen

1. Erzeuge eine Liste:

   ```csharp
   var allRows = mlContext.Data
       .CreateEnumerable<ModelInput>(allData, reuseRowObject: false)
       .OrderBy(r => r.Timestamp)
       .ToList();
   ```

2. Basiskontrollen:

   * `allRows.Count` loggen.
   * Min/Max Timestamp loggen.
   * Prüfen, ob `Stromverbrauch` irgendwo NaN ist oder negative Werte enthält.
   * Optional: prüfen, ob Zeitstempel stündlich lückenlos sind (Differenz zwischen aufeinanderfolgenden Timestamps = 1 Stunde). Bei Lücken könntest du später eine Forward‑Fill‑ or Interpolationslogik ergänzen; für erste Version kannst du annehmen, dass die E‑Control‑Daten sauber sind. ([Microsoft Learn][2])

---

## Phase 4 – Zeitlicher Split (Train vs. Test, Concept Drift beachten)

### 4.1 Split‑Strategie

Um **Concept Drift** (veraltete Muster, z. B. vor E‑Autos, Wärmepumpen) zu vermeiden, trainiere nur auf dem **letzten Jahr** vor dem Testzeitraum und ignoriere ältere Daten:

* **Trainingszeitraum:** `30.09.2023 00:00` bis `< 30.09.2024 00:00`.
* **Testzeitraum:** `>= 30.09.2024 00:00` bis Ende der Daten (ca. `30.09.2025 23:00`).

Daten **vor** `30.09.2023` werden **nicht** fürs Training verwendet, um Concept Drift zu minimieren.

### 4.2 Implementierung

```csharp
var trainStart = new DateTime(2023, 9, 30, 0, 0, 0);
var testStart  = new DateTime(2024, 9, 30, 0, 0, 0);

// Auf Trainings- und Testbereich beschränken
var trainList = allRows
    .Where(r => r.Timestamp >= trainStart && r.Timestamp < testStart)
    .ToList();

var testList = allRows
    .Where(r => r.Timestamp >= testStart)
    .ToList();

// IDataViews erstellen
IDataView trainData = mlContext.Data.LoadFromEnumerable(trainList);
IDataView testData  = mlContext.Data.LoadFromEnumerable(testList);
```

Keine zufälligen Splits, kein Shuffling – Zeitreihen immer chronologisch behandeln. ([Microsoft Learn][3])

---

## Phase 5 – SSA‑Forecasting‑Pipeline (ForecastBySsa)

### 5.1 Parameterwahl (auf Stunden gemappt)

Laut ML.NET‑Doku: ([Microsoft Learn][1])

* `windowSize` – Fensterlänge L (wie viele vergangene Punkte werden für ein Muster genutzt).
* `seriesLength` – Länge N der Serie im internen Puffer.
* `trainSize` – Anzahl Punkte vom Serienanfang im Trainingsset, die tatsächlich fürs Training verwendet werden.
* `horizon` – Anzahl zu prognostizierender Schritte.

Dein Setup (stündliche Daten):

* Auflösung: 1 Stunde.
* Dominante Rhythmen: Tages‑/Wochen‑Muster, lokaler Monatskontext, Jahreszyklus.

**Startkonfiguration:**

```csharp
int windowSize           = 7 * 24;   // 168 Stunden = 1 Woche
int seriesLength         = 30 * 24;  // 720 Stunden ≈ 1 Monat
int nominalTrainSize     = 365 * 24; // 8760 Stunden = 1 Jahr
int trainSize            = Math.Min(nominalTrainSize, trainList.Count);
int forecastHorizonHours = 24;       // 24h-Horizont für Punkt-Prognosen
float confidenceLevel    = 0.95f;
```

Hinweise aus Microsoft‑Q&A & Doku:

* `seriesLength` soll > `windowSize` und meist ca. 1.5–3× so groß sein; du liegst mit 720 vs. 168 etwas darüber, kannst das aber später feinjustieren. ([Microsoft Learn][4])
* `trainSize` darf nicht größer als `trainList.Count` sein und muss ≥ `seriesLength` sein.

### 5.2 Pipeline definieren

```csharp
var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
    outputColumnName: nameof(ModelOutput.ForecastedValues),
    inputColumnName:  nameof(ModelInput.Stromverbrauch),
    windowSize:       windowSize,
    seriesLength:     seriesLength,
    trainSize:        trainSize,
    horizon:          forecastHorizonHours,
    isAdaptive:       false,
    discountFactor:   1.0f,
    rankSelectionMethod: RankSelectionMethod.Exact,
    confidenceLowerBoundColumn: nameof(ModelOutput.LowerBoundValues),
    confidenceUpperBoundColumn: nameof(ModelOutput.UpperBoundValues),
    confidenceLevel:  confidenceLevel
);
```

* Advanced Parameter (`isAdaptive`, `discountFactor`, `rank`, `maxGrowth`, `shouldStabilize` etc.) zunächst bei **Default** lassen; SSA ist robust genug für eine erste Version. ([Microsoft Learn][1])

### 5.3 Modell trainieren

```csharp
var forecaster = forecastingPipeline.Fit(trainData);
```

---

## Phase 6 – Evaluation über den gesamten Testzeitraum + CSV‑Export

Du nutzt das Bike‑Tutorial als Vorlage: Transformiere `testData` mit dem Modell, vergleiche Ist‑Werte mit den `ForecastedValues[0]` und berechne MAE & RMSE.

### 6.1 Evaluation (MAE, RMSE, relative Fehler)

Implementiere:

```csharp
static void Evaluate(IDataView testData, ITransformer model, MLContext mlContext)
{
    IDataView predictions = model.Transform(testData);

    var actual = mlContext.Data
        .CreateEnumerable<ModelInput>(testData, reuseRowObject: false)
        .Select(r => r.Stromverbrauch)
        .ToArray();

    var forecasted = mlContext.Data
        .CreateEnumerable<ModelOutput>(predictions, reuseRowObject: false)
        .Select(p => p.ForecastedValues[0])
        .ToArray();

    var errors = actual.Zip(forecasted, (a, f) => a - f).ToArray();

    double mae  = errors.Average(e => Math.Abs(e));
    double rmse = Math.Sqrt(errors.Average(e => e * e));
    double meanLoad = actual.Average();

    Console.WriteLine("Evaluation Metrics");
    Console.WriteLine("---------------------");
    Console.WriteLine($"Mean Load (Test):      {meanLoad:F2} MW");
    Console.WriteLine($"Mean Absolute Error:   {mae:F2} MW ({mae / meanLoad:P1})");
    Console.WriteLine($"Root Mean Squared Err: {rmse:F2} MW ({rmse / meanLoad:P1})");
    Console.WriteLine();
}
```

Rufe `Evaluate(testData, forecaster, mlContext);` nach dem Training auf.

### 6.2 Detaillierter CSV‑Export für Excel (`evaluation_details.csv`)

Zusätzlich sollst du eine Datei `Data/evaluation_details.csv` erzeugen, um die Kurven in Excel plotten zu können.

Implementiere z. B.:

```csharp
static void ExportEvaluationDetails(
    IDataView testData,
    ITransformer model,
    MLContext mlContext,
    string exportPath)
{
    var predictions = model.Transform(testData);

    var actualRows = mlContext.Data
        .CreateEnumerable<ModelInput>(testData, reuseRowObject: false)
        .ToArray();

    var predRows = mlContext.Data
        .CreateEnumerable<ModelOutput>(predictions, reuseRowObject: false)
        .ToArray();

    using var writer = new StreamWriter(exportPath, false, Encoding.UTF8);
    writer.WriteLine("Timestamp;Actual_Consumption;Forecast_Value;Lower_Bound;Upper_Bound");

    int n = Math.Min(actualRows.Length, predRows.Length);

    for (int i = 0; i < n; i++)
    {
        var ts      = actualRows[i].Timestamp;
        float act   = actualRows[i].Stromverbrauch;
        float pred  = predRows[i].ForecastedValues[0];
        float lower = predRows[i].LowerBoundValues[0];
        float upper = predRows[i].UpperBoundValues[0];

        if (lower < 0) lower = 0; // negative Last verhindern

        writer.WriteLine(
            $"{ts:yyyy-MM-dd HH:mm};{act:F3};{pred:F3};{lower:F3};{upper:F3}");
    }
}
```

Ruf diese Methode nach `Evaluate(...)` auf, z. B.:

```csharp
ExportEvaluationDetails(testData, forecaster, mlContext,
    Path.Combine(rootDir, "Data", "evaluation_details.csv"));
```

Damit kannst du in Excel „Ist“ vs. „Forecast“ bequem visualisieren.

---

## Phase 7 – Zeitreihen‑Forecast für einen Zukunftshorizont

Optional, aber hilfreich: Erzeuge zusätzlich einen **reinen Zukunfts‑Forecast** mit einem `TimeSeriesPredictionEngine`, analog zum Bike‑Beispiel.

### 7.1 PredictionEngine & Modell speichern

```csharp
var forecastEngine = forecaster.CreateTimeSeriesEngine<ModelInput, ModelOutput>(mlContext);

string modelPath = Path.Combine(rootDir, "MLModel.zip");
forecastEngine.CheckPoint(mlContext, modelPath);
```

### 7.2 Forecast‑Funktion (nur Konsolenausgabe für z. B. nächste 168 Stunden)

```csharp
static void ForecastFuture(
    int horizon,
    TimeSeriesPredictionEngine<ModelInput, ModelOutput> forecastEngine)
{
    var forecast = forecastEngine.Predict();

    Console.WriteLine("Future Forecast");
    Console.WriteLine("TimestampIndex;Lower;Forecast;Upper");

    for (int i = 0; i < horizon; i++)
    {
        float lower = Math.Max(0, forecast.LowerBoundValues[i]);
        float pred  = forecast.ForecastedValues[i];
        float upper = forecast.UpperBoundValues[i];

        Console.WriteLine($"{i};{lower:F3};{pred:F3};{upper:F3}");
    }
}
```

Aufruf am Ende:

```csharp
ForecastFuture(forecastHorizonHours, forecastEngine);
```

---

## Phase 8 – Programmlogik zusammenführen

Bringe alles in `Program.cs` in eine klare Reihenfolge (Top‑Level Statements oder `Main`):

1. `using`‑Direktiven:

   * `System`, `System.IO`, `System.Linq`, `System.Globalization`, `System.Text`
   * `Microsoft.ML`, `Microsoft.ML.Data`, `Microsoft.ML.Transforms.TimeSeries`
2. Pfad‑Definitionen (`rootDir`, `dataDir`, `rawPath`, `cleanPath`, `modelPath`).
3. `var mlContext = new MLContext(seed: 0);`
4. Aufruf `CleanData(rawPath, cleanPath);`
5. Definition `ModelInput`, `ModelOutput`.
6. Konfiguration `TextLoader` und Laden von `el_power_clean.csv`.
7. Umwandlung in `allRows` + Quality Checks.
8. ZEIT‑Split in `trainData` und `testData` (Concept Drift beachten).
9. Konfiguration der SSA‑Pipeline (`ForecastBySsa` mit Stunden‑Parametern).
10. Training: `var forecaster = forecastingPipeline.Fit(trainData);`
11. Evaluation: `Evaluate(testData, forecaster, mlContext);`
12. Export: `ExportEvaluationDetails(testData, forecaster, mlContext, ...)`.
13. PredictionEngine & Modell‑Checkpoint.
14. Optionaler Zukunfts‑Forecast (`ForecastFuture`).

---

## Phase 9 – Typische Fehlerquellen explizit vermeiden

Stelle im Code sicher, dass Folgendes **nicht** passiert:

1. **Falsche Spalte gewählt**

   * Bei der Bereinigung wird Spalte 9 („Stromverbrauch“) aus der Rohdatei gezogen.
   * In `ModelInput` der bereinigten Datei gibt es **nur** `Timestamp` und `Stromverbrauch`.

2. **Dezimaltrennzeichen ignoriert**

   * In `CleanData` Komma → Punkt ersetzen.
   * In `TextLoader` `DecimalMarker = '.'` setzen.

3. **Zeitreihen‑Shuffle**

   * Keine zufälligen Splits; immer mit `Timestamp` filtern.

4. **Ungültige SSA‑Parameter**

   * Sicherstellen:

     * `windowSize < seriesLength <= trainSize`.
     * `trainSize <= trainList.Count`.
   * Sonst ggf. `trainSize = trainList.Count` und/oder `seriesLength` entsprechend verkleinern. ([Microsoft Learn][1])

5. **Zu großer Horizont**

   * Starte mit `horizon = 24` oder `168`. Je größer der Horizont, desto schlechter typischerweise die Genauigkeit bei univariater SSA. ([Microsoft Learn][4])

6. **Fehlende oder NaN‑Werte unbehandelt**

   * Falls im Clean‑Prozess Zeilen mit leerem Stromverbrauch nicht verworfen werden, nutze `ReplaceMissingValues` oder eine explizite Imputation, bevor du in die Pipeline gehst. ([Microsoft Learn][5])

---

## Phase 10 – Minimaldokumentation im Code

Füge kurze Kommentare ein, u. a.:

* Verweis auf das Bike‑Sharing‑Tutorial (als Pipeline‑Vorlage).
* Ein Satz zu SSA (Zerlegung in Trend/Saisonalität/Rauschen, Trajektorienmatrix).
* Begründung der Parameter:

  * `windowSize = 168` → Wochenmuster (Mo–So).
  * `seriesLength ≈ 30 Tage` → lokaler Monatskontext.
  * `trainSize ≈ 1 Jahr` → kompletter Jahreszyklus.

---

[1]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.timeseriescatalog.forecastbyssa?view=ml-dotnet-preview&utm_source=chatgpt.com "TimeSeriesCatalog.ForecastBySsa Method (Microsoft.ML)"
[2]: https://learn.microsoft.com/en-us/dotnet/machine-learning/resources/transforms?utm_source=chatgpt.com "Data transformations - ML.NET"
[3]: https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/time-series-demand-forecasting?utm_source=chatgpt.com "Tutorial: Forecast bike rental demand - time series - ML.NET"
[4]: https://learn.microsoft.com/en-us/answers/questions/2181403/ml-net-time-series-algorithms-to-predict-future-fo?utm_source=chatgpt.com "ML.NET Time Series algorithms to predict future forecasts"
[5]: https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/prepare-data-ml-net?utm_source=chatgpt.com "Prepare data for building a model - ML.NET"
