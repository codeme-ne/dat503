using Microsoft.ML.Data;

namespace PowerDemandForecasting.Models;

/// <summary>
/// Output schema for SSA forecasting results
/// Contains forecasted values and confidence interval bounds
/// </summary>
public class ModelOutput
{
    [ColumnName("ForecastedValues")]
    public float[] ForecastedValues { get; set; } = Array.Empty<float>();

    [ColumnName("LowerBoundValues")]
    public float[] LowerBoundValues { get; set; } = Array.Empty<float>();

    [ColumnName("UpperBoundValues")]
    public float[] UpperBoundValues { get; set; } = Array.Empty<float>();
}
