# ⚡ Power Demand Forecasting (Austria)

Eine auf **.NET 8** und **ML.NET** basierende Konsolenanwendung zur stündlichen Vorhersage des österreichischen Stromverbrauchs unter Verwendung der **Singular Spectrum Analysis (SSA)**.

-----

## 📑 Inhaltsverzeichnis

  - [Über das Projekt](https://www.google.com/search?q=%23-%C3%BCber-das-projekt)
  - [Features & Methodik](https://www.google.com/search?q=%23-features--methodik)
  - [Pipeline-Architektur](https://www.google.com/search?q=%23-pipeline-architektur)
  - [Ergebnisse & Evaluation](https://www.google.com/search?q=%23-ergebnisse--evaluation)
  - [Voraussetzungen & Installation](https://www.google.com/search?q=%23-voraussetzungen--installation)
  - [Nutzung](https://www.google.com/search?q=%23-nutzung)
  - [Projektstruktur](https://www.google.com/search?q=%23-projektstruktur)
  - [Referenzen](https://www.google.com/search?q=%23-referenzen)

-----

## 📖 Über das Projekt

Ziel dieses Projekts ist es, den stündlichen Stromverbrauch (Last) in Österreich für einen zukünftigen Horizont von 24 Stunden vorherzusagen. Die Daten stammen von der [E-Control](https://www.e-control.at/statistik/e-statistik/data).

Besonderes Augenmerk liegt auf der **Datenqualität** und dem Umgang mit realen Problemen von Zeitreihen, wie der Sommer-/Winterzeitumstellung (DST), fehlenden Werten und Concept Drift. Als Algorithmus wird SSA verwendet, da dieser univariate Zeitreihen effektiv in Trend-, Saison- und Rauschkomponenten zerlegen kann, ohne auf externe Wetterdaten angewiesen zu sein.

-----

## ✨ Features & Methodik

### 🧠 Algorithmus: Singular Spectrum Analysis (SSA)

Das Modell nutzt die Zeitreihen-Dekomposition von Microsoft.ML.TimeSeries.

  - **Window Size:** `168` Stunden (bildet den wöchentlichen Zyklus ab).
  - **Series Length:** `720` Stunden (bildet den monatlichen Kontext ab).
  - **Train Size:** \~1 Jahr (dynamisch berechnet, um Concept Drift zu minimieren).
  - **Confidence:** 95% (berechnet untere und obere Prognoseschranken).

### 🛠️ Robustes Data Engineering

Das Projekt implementiert eine fortgeschrittene Logik zur Behandlung von Zeitumstellungen (DST):

  * **Oktober-Duplikate (Uhren zurück):** Erkennt doppelte Zeitstempel (z.B. 02:00A und 02:00B) und berechnet den Mittelwert, um die Monotonie der Zeitreihe zu wahren.
  * **März-Lücken (Uhren vor) & Ausfälle:** Erkennt fehlende Stunden und füllt diese mittels **linearer Interpolation** basierend auf den Nachbarwerten auf.

### 📅 Concept Drift Prevention

Um veraltete Verbrauchsmuster (z.B. vor dem Anstieg von E-Mobilität und Wärmepumpen) auszuschließen, wird ein strikter **temporaler Split** angewendet:

  * **Training:** 30.09.2023 – 30.09.2024
  * **Testing:** 30.09.2024 – 30.09.2025

-----

## ⚙️ Pipeline-Architektur

Der Prozess läuft vollautomatisch in `Program.cs` ab:

```mermaid
graph TD;
    Raw[Raw CSV (E-Control)] -->|CleanData| Clean[Clean CSV (Format Fix)];
    Clean -->|HandleDst| DST[DST Fixed CSV (No Gaps/Dups)];
    DST -->|Split| Train[Train Data (2023-24)];
    DST -->|Split| Test[Test Data (2024-25)];
    Train -->|TrainModel| Model[SSA Model (.zip)];
    Test -->|Evaluate| Metrics[MAE / RMSE];
    Model -->|Transform| Export[Evaluation Details (.csv)];
```

1.  **CleanData:** Parsing der Rohdaten, Entfernung von Metadaten, Normalisierung von Dezimaltrennzeichen (Komma → Punkt).
2.  **QualityChecks:** Prüfung auf NaN, negative Werte und Zeitstempel-Integrität.
3.  **HandleDstDuplicatesAndGaps:** Korrektur von Zeitumstellungs-Anomalien.
4.  **CreateTrainTestFiles:** Physische Trennung der Daten.
5.  **TrainModel:** Training des SSA-Modells.
6.  **EvaluateAndExport:** Berechnung der Metriken und Export der Prognosewerte vs. Ist-Werte.

-----

## 📊 Ergebnisse & Evaluation

Das Modell wurde auf einem ungesehenen Testzeitraum (Sept 2024 - Sept 2025) evaluiert.

| Metrik | Wert | Bedeutung |
| :--- | :--- | :--- |
| **Mean Load** | `6662.50 MW` | Durchschnittliche Last im Testzeitraum |
| **MAE** | `261.16 MW` | Mittlerer absoluter Fehler (\~3.92%) |
| **RMSE** | `339.40 MW` | Wurzel des mittleren quadratischen Fehlers |

Die Ergebnisse zeigen eine relative Abweichung von **unter 4%**, was für eine univariate Prognose ohne Wetterdaten als robust gilt. Detaillierte Ergebnisse pro Stunde finden sich nach dem Ausführen in `Data/evaluation_details.csv`.

-----

## 💻 Voraussetzungen & Installation

### Voraussetzungen

  - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installiert.
  - Git.

### Installation

1.  Repository klonen:

    ```bash
    git clone https://github.com/dein-user/dat503-power-forecasting.git
    cd dat503-power-forecasting
    ```

2.  Abhängigkeiten wiederherstellen:

    ```bash
    dotnet restore PowerDemandForecasting/PowerDemandForecasting.csproj
    ```

-----

## 🚀 Nutzung

Das Projekt ist als Konsolenanwendung konzipiert, die die gesamte Pipeline sequenziell durchläuft.

### Build

```bash
dotnet build PowerDemandForecasting/PowerDemandForecasting.csproj
```

### Ausführen

```bash
dotnet run --project PowerDemandForecasting/PowerDemandForecasting.csproj
```

### Output

Nach erfolgreicher Ausführung findest du im Ordner `PowerDemandForecasting/Data/`:

  - `el_power_clean_dstfixed.csv`: Die bereinigte, lückenlose Zeitreihe.
  - `evaluation_details.csv`: CSV mit Spalten `Timestamp`, `Actual`, `Forecast`, `LowerBound`, `UpperBound` (ideal für Analysen in Excel/PowerBI).
  - `train_data.csv` / `test_data.csv`: Die verwendeten Datensätze.

Das trainierte Modell wird unter `PowerDemandForecasting/Models/forecast_model.zip` gespeichert.

-----

## 📂 Projektstruktur

```text
PowerDemandForecasting/
├── Data/                           # Input- & Output-Daten
│   ├── el_dataset_h.csv            # Rohdaten (Input)
│   ├── el_power_clean.csv          # Zwischenschritt (Format bereinigt)
│   └── evaluation_details.csv      # Endergebnis (Forecast vs Actual)
├── Models/                         # C# Klassen & Modell-Artefakte
│   ├── ModelInput.cs               # Daten-Schema
│   ├── ModelOutput.cs              # Prognose-Schema
│   └── forecast_model.zip          # Trainiertes ML.NET Modell
├── Program.cs                      # Hauptlogik (Pipeline)
├── PowerDemandForecasting.csproj   # Projektkonfiguration
├── AGENTS.md                       # Richtlinien für AI-Assistenten
└── README.md                       # Projektdokumentation
```

-----

## 📚 Referenzen

  * [Microsoft ML.NET Time Series Forecasting Tutorial](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/time-series-demand-forecasting)
  * [Singular Spectrum Analysis (SSA) - Wikipedia](https://en.wikipedia.org/wiki/Singular_spectrum_analysis)
  * [E-Control Austria (Datenquelle)](https://www.e-control.at/)
