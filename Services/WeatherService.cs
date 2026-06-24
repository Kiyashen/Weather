using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Weather.Models.API;

namespace Weather.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        // The constructor automatically extracts your keys from appsettings.json safely!
        public WeatherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["WeatherApiSettings:ApiKey"];
            _baseUrl = configuration["WeatherApiSettings:BaseUrl"];
        }

        // 1. Fetches Current Weather for a searched city
        public async Task<CurrentWeatherResponse> GetCurrentWeatherAsync(string city)
        {
            // Built-in safety check for empty searches
            if (string.IsNullOrWhiteSpace(city)) return null;

            // Constructs the full URL request path (using metric units for Celsius)
            string url = $"{_baseUrl}weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null; // Let Member 4 handle specific 404 errors later

                var jsonString = await response.Content.ReadAsStringAsync();

                // Converts the raw web string into your structured C# object!
                return JsonSerializer.Deserialize<CurrentWeatherResponse>(jsonString);
            }
            catch (Exception)
            {
                return null; // Safe fallback if network drops completely
            }
        }

        // 2. Fetches the 5-Day / 3-Hour Forecast for a searched city
        public async Task<ForecastWeatherResponse> GetForecastWeatherAsync(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return null;

            string url = $"{_baseUrl}forecast?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var jsonString = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ForecastWeatherResponse>(jsonString);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
