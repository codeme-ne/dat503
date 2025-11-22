using Microsoft.ML.Data;

namespace PowerDemandForecasting.Models;

/// <summary>
/// Input schema for reading cleaned CSV data from el_power_clean.csv
/// Maps to columns: Timestamp;Stromverbrauch
/// </summary>
public class ModelInput
{
    [LoadColumn(0)]
    [ColumnName("Timestamp")]
    public DateTime Timestamp { get; set; }

    [LoadColumn(1)]
    [ColumnName("Stromverbrauch")]
    public float Stromverbrauch { get; set; }
}
