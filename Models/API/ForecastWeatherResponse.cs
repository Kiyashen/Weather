using System.Text.Json.Serialization;
namespace Weather.Models.API
{
    public class ForecastWeatherResponse
    {
        [JsonPropertyName("list")]
        public List<ForecastItem> ForecastList { get; set; }

        [JsonPropertyName("city")]
        public ForecastCity City { get; set; }
    }

    // Every item in the list represents a weather snapshot 3 hours apart
    public class ForecastItem
    {
        [JsonPropertyName("dt")]
        public long DateTimeUnix { get; set; } // Unix timestamp (important for Member 2 to parse days)

        [JsonPropertyName("main")]
        public MainData Main { get; set; } // Reuses the MainData class we made in our other file!

        [JsonPropertyName("weather")]
        public List<WeatherCondition> Weather { get; set; } // Reuses WeatherCondition!

        [JsonPropertyName("dt_txt")]
        public string DateTimeText { get; set; } // Formatted text time string like "2026-06-25 12:00:00"
    }

    public class ForecastCity
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }


}
