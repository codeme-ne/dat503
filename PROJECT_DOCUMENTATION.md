# Projektdokumentation – Power Demand Forecasting Austria
**Repo:** `codeme-ne/dat503` | **Bewerbung:** Automation & KI Experte @ Veritas

---

## 1. Projektname & Kurzbeschreibung

**Name:** Power Demand Forecasting Austria  
**Repository:** [github.com/codeme-ne/dat503](https://github.com/codeme-ne/dat503)

Eine **vollautomatische ML-Pipeline** zur stündlichen Vorhersage des österreichischen Stromverbrauchs (MW) mit einem 24-Stunden-Horizont. Die Anwendung verarbeitet ~85 000 Rohdatensätze, bereinigt diese mit produktionsreifer Qualitätssicherung (inkl. Zeitumstellungskorrektur), trainiert ein Singular-Spectrum-Analysis-Modell (SSA) und exportiert detaillierte Evaluierungsergebnisse – **alles mit einem einzigen Konsolenbefehl.**

---

## 2. Verwendeter Stack

| Kategorie | Technologie |
|-----------|-------------|
| **Sprache / Framework** | C# 12, .NET 8 |
| **ML-Bibliothek** | Microsoft ML.NET 5.0.0 |
| **ML-Algorithmus** | Singular Spectrum Analysis (SSA) – `ForecastBySsa` |
| **Zeitreihen-Extension** | Microsoft.ML.TimeSeries 5.0.0 |
| **Datenhaltung** | CSV (E-Control Austria Open Data) |
| **Build / Tooling** | .NET CLI (`dotnet build / run / test`) |
| **Versionierung** | Git / GitHub |
| **Dokumentation** | Markdown, Mermaid-Diagramme |

---

## 3. Ziele

### Business-Ziel
Bereitstellung einer robusten, reproduzierbaren **Day-Ahead Lastprognose** für den österreichischen Strommarkt – einsetzbar für Grid-Dispatching, Marktgebote und Lastverteilung.

### Technische Ziele
1. **End-to-End-Automatisierung** – von Rohdaten bis zum bewerteten Modell in einer Pipeline
2. **Produktionsreife Datenqualität** – Behandlung realer Messprobleme (Zeitumstellung, fehlende Werte, Concept Drift)
3. **Reproduzierbarkeit** – deterministisches Training (`MLContext(seed: 0)`), non-destructive Zwischendateien
4. **Quantifizierte Unsicherheit** – 95%-Konfidenzintervalle für jede Prognose
5. **Zukunftssicherheit** – modulare, erweiterbare Architektur (weitere Features, Regionen, Algorithmen)

---

## 4. Umsetzung – Architektur & Pipeline

```
el_dataset_h.csv (Rohdaten, ~85k Zeilen)
       │
       ▼  Phase 1: Data Cleaning
el_power_clean.csv          ← Metadaten entfernt, Spaltenextraktion, Dezimalkonvertierung
       │
       ▼  Phase 2: Quality Checks & DST Fixing
el_power_clean_dstfixed.csv ← Duplikate gemittelt, Lücken interpoliert, Monotonie garantiert
       │
       ├──▶  train_data.csv  (Phase 3: 2023-09-30 – 2024-09-30, ~8 784 h)
       └──▶  test_data.csv   (Phase 3: 2024-09-30 – 2025-09-30, ~8 760 h)
                   │
                   ▼  Phase 4: SSA Model Training
              forecast_model.zip   (windowSize=168h, seriesLength=720h, horizon=24h)
                   │
                   ▼  Phase 5: Rolling-Origin Evaluation
              evaluation_details.csv  (121 Spalten: Actual / Forecast / Error / CI je Horizont)
```

### Schlüsselimplementierungen

#### DST-Korrektur (Daylight Saving Time)
- **Oktober (Uhr zurück):** Doppelte Zeitstempel erkannt → Mittelwert berechnet → Monotonie gewahrt
- **März (Uhr vor):** Fehlende Stunde erkannt → Lineare Interpolation aus Nachbarpunkten eingefügt

#### Concept Drift Prevention
Trainiert **ausschließlich auf dem letzten vollen Jahr** (2023–2024), da ältere Daten veraltete Muster widerspiegeln (Vor-E-Mobilitäts-Ära, vor Wärmepumpen-Boom, COVID-Anomalien 2020–2021).

#### SSA-Parameterkonfiguration

| Parameter | Wert | Begründung |
|-----------|------|-----------|
| `windowSize` | 168 h (7 Tage) | Wöchentlicher Verbrauchszyklus |
| `seriesLength` | 720 h (30 Tage) | Monatlicher Kontext für Dekomposition |
| `trainSize` | dynamisch (~8 784 h) | Dynamisch berechnet aus tatsächlicher Trainingsmenge (inkl. Schalttag 2024) |
| `horizon` | 24 h | Day-Ahead Marktstandard |
| `confidenceLevel` | 0.95 | 95%-Konfidenzintervalle |

#### Rolling-Origin Evaluation
Statt einfacher Batch-Prognose wird eine **stateful Rolling-Origin Methode** eingesetzt: Für jeden Zeitpunkt im Testset wird eine neue 24-Stunden-Prognose erstellt und das Modell mit dem echten Istwert aktualisiert (`TimeSeriesPredictionEngine`) – kein Data Leakage, realistische Leistungsmessung.

---

## 5. Vorher / Nachher – Ergebnisse

### Datenzustand: Vorher → Nachher

| Eigenschaft | Rohdaten | Nach Pipeline |
|-------------|----------|---------------|
| Format | Semikolon-CSV, Komma-Dezimal, 14 Metazeilen | Sauberes 2-Spalten-CSV, Punkt-Dezimal |
| Zeitreihenintegrität | ~2 DST-Duplikate/Jahr + ~1–2 Lücken/Jahr (gesamt 9 Jahre: ~18–28 Anomalien) | Lückenlos, monoton, keine Duplikate |
| Datensätze | ~85 463 (2016–2025) | 8 784 Train / 8 760 Test (2023–2025) |
| Nutzbarkeit für ML | ❌ Direkt nicht parsebar | ✅ ML.NET TextLoader-kompatibel |

### Modellleistung: Per-Horizon Metriken (Testset 2024–2025)

| Horizont | MAE (MW) | RMSE (MW) | MAPE (%) |
|----------|----------|-----------|----------|
| **1 h** | 284.7 | 374.2 | 4.29% |
| **3 h** | 381.1 | 498.4 | 5.73% |
| **6 h** | 418.8 | 552.1 | 6.28% |
| **12 h** | 448.2 | 589.7 | 6.77% |
| **24 h** | 450.6 | 601.3 | 6.76% |
| **Gesamt (Ø)** | **426.7** | **564.2** | **6.42%** |

**Referenzwert:** Mittlerer Stromverbrauch (Testperiode) = **6 662 MW**  
**Bewertung:** 6.42% MAPE für **univariates Day-Ahead Forecasting ohne Wetterdaten** ist sehr gut und liegt im wettbewerbsfähigen Bereich. Vergleichbare kommerzielle Systeme mit Wetterdaten erreichen 3–5%.

### Erkenntnisse aus der Evaluation
- **Stärkster Genauigkeitsverlust in Stunde 1–6** (4.29% → 6.28%), danach Stabilisierung
- **Plateau ab Horizont 12h** (MAPE ~6.7–6.8%) → SSA erfasst Tagesmuster sehr gut
- **RMSE > MAE** zeigt vereinzelte Ausreißer (Extremereignisse), aber grundsätzlich stabile Performance

---

## 6. Herausforderungen & Lösungen

| Challenge | Problem | Lösung |
|-----------|---------|--------|
| **DST-Anomalien** | Duplikate (Oktober) & Lücken (März) brechen SSA-Annahmen | Intelligente 2-Stufen-Korrektur: Mittelwert + lineare Interpolation |
| **Concept Drift** | 8 Jahre Rohdaten enthalten veraltete Muster | Temporaler Split: nur letztes Jahr für Training |
| **Dezimaltrennzeichen** | E-Control CSV: Komma; ML.NET erwartet Punkt | `CultureInfo.InvariantCulture` konsequent in gesamter Pipeline |
| **Schaltjahr** | 2024 hat 8 784 h statt 8 760 h | Dynamische `trainSize`-Berechnung zur Laufzeit |
| **Data Leakage** | Einfache Batch-Evaluation überoptimistisch | Rolling-Origin mit `TimeSeriesPredictionEngine` statt `Transform()` |

---

## 7. Projektstruktur

```
dat503/
├── PowerDemandForecasting/
│   ├── Program.cs              # Gesamte Pipeline-Logik (~950 Zeilen)
│   ├── Models/
│   │   ├── ModelInput.cs       # ML.NET Input-Schema
│   │   └── ModelOutput.cs      # ML.NET Output-Schema (Forecast + CI)
│   └── Data/                   # Input/Output CSV-Dateien
├── README.md                   # Technische Vollständige Dokumentation
├── PLAN.md                     # Chronologischer Entwicklungsplan
└── PROJECT_DOCUMENTATION.md   # Diese Datei (Portfolio/Bewerbung)
```

---

## 8. Erweiterbarkeit & Produktionspfad

| Nächster Schritt | Beschreibung |
|-----------------|-------------|
| **Multivariate Features** | Temperatur, Feiertage, Wochentag via `CustomMapping` oder Regression integrieren |
| **REST-API** | Modell-Checkpoint in ASP.NET Core Minimal API kapseln |
| **Azure Integration** | Azure ML, Azure Functions oder Azure Stream Analytics für Real-Time-Inferenz |
| **Ensemble** | SSA für <12h, ARIMA/Prophet für >12h für optimale Horizont-spezifische Performance |
| **AutoML** | ML.NET Model Builder für systematisches Hyperparameter-Tuning |
| **Monitoring** | Concept-Drift-Detektion + automatisches Retraining (monatlich/jährlich) |

---

## 9. Relevanz für Automation & KI Expertise

Dieses Projekt demonstriert folgende Kernkompetenzen, die für eine Stelle als **Automation & KI Experte** direkt relevant sind:

- ✅ **End-to-End ML-Pipelines** – Design, Implementierung und Evaluation vollständiger ML-Workflows
- ✅ **Produktionsreifes Data Engineering** – Robuste Datenbereinigung für Realweltprobleme (DST, Concept Drift, fehlende Werte)
- ✅ **Zeitreihenanalyse** – SSA-basierte Dekomposition und Forecasting mit bewiesener Produktionsqualität
- ✅ **Automatisierung** – Vollständig automatisierte Pipeline von Rohdaten bis zum Evaluierungsreport
- ✅ **Quantitatives Denken** – Rolling-Origin Evaluation, Per-Horizon Metriken, Unsicherheitsquantifizierung
- ✅ **Clean Code & Dokumentation** – Modulare Architektur, umfassende Kommentierung, reproduzierbare Ergebnisse

---

*Entwickelt mit .NET 8 & ML.NET 5.0.0 | Datenquelle: E-Control Austria (Open Data)*
