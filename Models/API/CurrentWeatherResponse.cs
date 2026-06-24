using System.Text.Json.Serialization;
namespace Weather.Models.API

{
    public class CurrentWeatherResponse
    {
        [JsonPropertyName("name")]
        public string CityName { get; set; }

        [JsonPropertyName("main")]
        public MainData Main { get; set; }

        [JsonPropertyName("weather")]
        public List<WeatherCondition> Weather { get; set; }

        [JsonPropertyName("wind")]
        public WindData Wind { get; set; }

        [JsonPropertyName("sys")]
        public SysData Sys { get; set; }
    }

    // This block extracts temperature metrics
    public class MainData
    {
        [JsonPropertyName("temp")]
        public double Temperature { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }
    }

    // This block extracts details for descriptive badges and icons
    public class WeatherCondition
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("icon")]
        public string IconCode { get; set; }

        [JsonPropertyName("id")]
        public int ConditionId { get; set; } // Member 3 needs this to set theme colors!
    }

    // This block extracts wind elements for outdoor site safety checks
    public class WindData
    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; }
    }

    // This block identifies geographical origin markers
    public class SysData
    {
        [JsonPropertyName("country")]
        public string Country { get; set; }
    }
}

