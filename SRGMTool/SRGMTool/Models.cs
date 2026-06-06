using System.Collections.Generic;
using Newtonsoft.Json;

namespace SRGMTool
{
    public class ModelMetrics
    {
        [JsonProperty("AIC")]
        public double AIC { get; set; }
        [JsonProperty("BIC")]
        public double BIC { get; set; }
        [JsonProperty("RSquared")]
        public double RSquared { get; set; }
        [JsonProperty("AdjRSquared")]
        public double AdjRSquared { get; set; }
    }

    public class ModelResult
    {
        [JsonProperty("params")]
        public Dictionary<string, double>? Params { get; set; }
        [JsonProperty("metrics")]
        public ModelMetrics? Metrics { get; set; }
        [JsonProperty("curve")]
        public List<List<double>>? Curve { get; set; }
        [JsonProperty("error")]
        public string? Error { get; set; }
    }

    public class AnalysisResult
    {
        [JsonProperty("models")]
        public Dictionary<string, ModelResult>? Models { get; set; }
        [JsonProperty("best_model")]
        public string? BestModel { get; set; }
        [JsonProperty("data")]
        public List<List<double>>? Data { get; set; }
        [JsonProperty("predictions")]
        public List<List<double>>? Predictions { get; set; }
        [JsonProperty("error")]
        public string? Error { get; set; }
    }
}
