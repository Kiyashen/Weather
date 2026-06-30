
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Weather.Models.API;
using Weather.Services;

namespace Weather.Controllers
{
    public class WeatherController : Controller
    {
        private readonly WeatherService _weatherService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        // Session keys as constants
        private const string SEARCH_HISTORY_KEY = "RecentSearches";
        private const string TEMP_UNIT_KEY = "TemperatureUnit";

        // Automatically injects your API service, along with Member 2's cache and configuration systems
        public WeatherController(WeatherService weatherService, IMemoryCache cache, IConfiguration configuration)
        {
            _weatherService = weatherService;
            _cache = cache;
            _configuration = configuration;
        }

        // ======================================================================
        // GET: /Weather
        // TASK: SESSION INITIALIZATION
        // ======================================================================
        [HttpGet]
        public IActionResult Index()
        {
            // ==========================================
            // MEMBER 2: TASK - SESSION INITIALIZATION
            // ==========================================
            // Read temperature unit state toggle (Celsius vs Fahrenheit) from Session State.
            // Pass recent 5-city history list to ViewBag so Member 3 can display clickable quick-links.

            // Initialize temperature unit if not set (default to Celsius)
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(TEMP_UNIT_KEY)))
            {
                HttpContext.Session.SetString(TEMP_UNIT_KEY, "Celsius");
            }

            // Get search history and temperature unit for display
            ViewBag.RecentSearches = GetSearchHistory();
            ViewBag.TemperatureUnit = GetTemperatureUnit();

            return View();
        }

        // ======================================================================
        // GET: /Weather/Search?city=Durban
        // TASKS: IMEMORYCACHE INTERCEPT LAYER + SESSION ARRAY TRACKING
        // ======================================================================
        [HttpGet]
        public async Task<IActionResult> Search(string city)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(city))
            {
                ViewBag.ErrorMessage = "Please enter a valid city name.";
                ViewBag.RecentSearches = GetSearchHistory();
                ViewBag.TemperatureUnit = GetTemperatureUnit();
                return View("Index");
            }

            string cleanCityKey = city.Trim().ToLower();

            // ======================================================================
            // MEMBER 2: TASK - READ EXPIRY FROM CONFIGURATION
            // ======================================================================
            // Reads dynamically from your existing setup: "WeatherApiSettings" -> "CacheExpiryMinutes"
            var expiryMinutes = _configuration.GetValue<int>("WeatherApiSettings:CacheExpiryMinutes", 30);

            // Create placeholders for our data objects using your models
            CurrentWeatherResponse currentData = null;
            ForecastWeatherResponse forecastData = null;

            // ======================================================================
            // MEMBER 2: TASK - IMEMORYCACHE INTERCEPT LAYER
            // ======================================================================
            // 1. Check if 'cleanCityKey' data already exists inside _cache.
            // 2. If it EXISTS: Load it instantly from memory to save API requests.
            // 3. If it DOES NOT EXIST: Run the live API block below, then save the data into _cache using the expiryMinutes setting.

            // Create cache keys
            string cacheKey = $"WeatherData_{cleanCityKey}";
            string rawCacheKey = $"WeatherData_Raw_{cleanCityKey}";

            // Try to get cached data (could be in Celsius or Fahrenheit depending on user preference)
            if (_cache.TryGetValue(cacheKey, out (CurrentWeatherResponse Current, ForecastWeatherResponse Forecast) cachedData))
            {
                // Cache hit! Use cached data
                currentData = cachedData.Current;
                forecastData = cachedData.Forecast;
            }
            else
            {
                // --- LIVE API CALL BLOCK ---
                // WeatherService always returns data in Celsius (units=metric)
                currentData = await _weatherService.GetCurrentWeatherAsync(city);
                forecastData = await _weatherService.GetForecastWeatherAsync(city);
                // ---------------------------

                if (currentData != null && forecastData != null)
                {
                    // Store raw Celsius data permanently (for temperature toggling without re-querying API)
                    var rawCurrentClone = DeepClone(currentData);
                    var rawForecastClone = DeepClone(forecastData);
                    _cache.Set(rawCacheKey, (rawCurrentClone, rawForecastClone), TimeSpan.FromMinutes(expiryMinutes));

                    // Store the data (may be converted to Fahrenheit based on user preference)
                    _cache.Set(cacheKey, (currentData, forecastData), TimeSpan.FromMinutes(expiryMinutes));
                }
            }

            if (currentData == null || forecastData == null)
            {
                ViewBag.ErrorMessage = $"Could not retrieve weather data for '{city}'. Please check the spelling.";
                ViewBag.RecentSearches = GetSearchHistory();
                ViewBag.TemperatureUnit = GetTemperatureUnit();
                return View("Index");
            }

            // ======================================================================
            // MEMBER 2: TASK - SESSION ARRAY TRACKING (LAST 5 CITIES)
            // ======================================================================
            // 1. Retrieve the existing list of tracked cities from HttpContext.Session.
            // 2. Append the current city name to the collection array.
            // 3. Keep ONLY the last 5 unique searched cities in the key-value array.
            // 4. Save the updated list back into the active session block.
            UpdateSearchHistory(city);

            // ======================================================================
            // MEMBER 2: TASK - TEMPERATURE CONVERSION FOR DISPLAY
            // ======================================================================
            // Apply temperature conversion based on current unit setting
            var currentUnit = GetTemperatureUnit();

            if (currentUnit == "Fahrenheit")
            {
                // Convert current weather temps (Celsius to Fahrenheit)
                if (currentData.Main != null)
                {
                    currentData.Main.Temperature = CelsiusToFahrenheit(currentData.Main.Temperature);
                    currentData.Main.FeelsLike = CelsiusToFahrenheit(currentData.Main.FeelsLike);
                }

                // Convert forecast temps
                if (forecastData.ForecastList != null)
                {
                    foreach (var item in forecastData.ForecastList)
                    {
                        if (item.Main != null)
                        {
                            item.Main.Temperature = CelsiusToFahrenheit(item.Main.Temperature);
                            item.Main.FeelsLike = CelsiusToFahrenheit(item.Main.FeelsLike);
                        }
                    }
                }
            }
            // else: Keep as Celsius (already in Celsius from API)

            // Package payloads up for the view
            ViewBag.Current = currentData;
            ViewBag.Forecast = forecastData;
            ViewBag.SearchedCity = city;
            ViewBag.RecentSearches = GetSearchHistory();
            ViewBag.TemperatureUnit = currentUnit;

            return View("Index");
        }

        // ======================================================================
        // POST Action endpoint for Temperature Scale Toggle
        // TASKS: TEMPERATURE SCALE TOGGLE CALCULATIONS
        // ======================================================================
        [HttpPost]
        public IActionResult ToggleUnits(string currentCity, string unit)
        {
            // ======================================================================
            // MEMBER 2: TASK - TEMPERATURE SCALE TOGGLE CALCULATIONS
            // ======================================================================
            // 1. Save chosen unit string ("C" or "F") into Session State tracking parameter.
            // 2. If a city search is currently active, load its data directly from the IMemoryCache layers.
            // 3. Execute conversion mathematical formulas purely on the backend server engine to convert values WITHOUT re-pinging the OpenWeatherMap API endpoints.

            // Save the new unit preference
            string newUnit = string.IsNullOrEmpty(unit) ? "Fahrenheit" : unit;
            HttpContext.Session.SetString(TEMP_UNIT_KEY, newUnit);

            // If we have a current city, reload from cache and convert
            if (!string.IsNullOrEmpty(currentCity))
            {
                string cleanCityKey = currentCity.Trim().ToLower();
                string cacheKey = $"WeatherData_{cleanCityKey}";
                string rawCacheKey = $"WeatherData_Raw_{cleanCityKey}";

                // Try to get raw Celsius data first (always stored in Celsius)
                if (_cache.TryGetValue(rawCacheKey, out (CurrentWeatherResponse Current, ForecastWeatherResponse Forecast) rawData))
                {
                    // We have raw Celsius data - create deep copies and convert
                    var currentData = DeepClone(rawData.Current);
                    var forecastData = DeepClone(rawData.Forecast);

                    if (newUnit == "Fahrenheit")
                    {
                        // Convert from Celsius to Fahrenheit
                        if (currentData.Main != null)
                        {
                            currentData.Main.Temperature = CelsiusToFahrenheit(currentData.Main.Temperature);
                            currentData.Main.FeelsLike = CelsiusToFahrenheit(currentData.Main.FeelsLike);
                        }

                        if (forecastData.ForecastList != null)
                        {
                            foreach (var item in forecastData.ForecastList)
                            {
                                if (item.Main != null)
                                {
                                    item.Main.Temperature = CelsiusToFahrenheit(item.Main.Temperature);
                                    item.Main.FeelsLike = CelsiusToFahrenheit(item.Main.FeelsLike);
                                }
                            }
                        }
                    }
                    // else: Keep as Celsius (already in Celsius from raw data)

                    // Cache the converted data for faster subsequent access
                    _cache.Set(cacheKey, (currentData, forecastData), TimeSpan.FromMinutes(30));

                    // Update ViewBag with converted data
                    ViewBag.Current = currentData;
                    ViewBag.Forecast = forecastData;
                    ViewBag.SearchedCity = currentCity;
                }
                else
                {
                    // Raw data not in cache - try to get converted data
                    if (_cache.TryGetValue(cacheKey, out (CurrentWeatherResponse Current, ForecastWeatherResponse Forecast) cachedData))
                    {
                        // We have cached data but need to ensure correct unit
                        // Since we don't know what unit it's in, we'll redirect to fetch fresh
                        return RedirectToAction("Search", new { city = currentCity });
                    }
                    else
                    {
                        // No cache at all - redirect to fetch
                        return RedirectToAction("Search", new { city = currentCity });
                    }
                }
            }

            ViewBag.RecentSearches = GetSearchHistory();
            ViewBag.TemperatureUnit = GetTemperatureUnit();

            if (!string.IsNullOrEmpty(currentCity))
            {
                return View("Index");
            }

            return RedirectToAction("Index");
        }

        // ======================================================================
        //  Clickable history links
        // ======================================================================
        [HttpGet]
        public IActionResult SearchHistory(string city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return BadRequest("City name is required");
            }

            // Redirect to Search with the city parameter
            return RedirectToAction(nameof(Search), new { city = city });
        }

        // ======================================================================
        // MEMBER 2: HELPER METHODS
        // ======================================================================

        /// <summary>
        /// Updates the last 5 searched cities in Session
        /// </summary>
        private void UpdateSearchHistory(string city)
        {
            if (string.IsNullOrEmpty(city)) return;

            var history = GetSearchHistory();

            // Remove if city already exists (to avoid duplicates)
            history.RemoveAll(c => c.Equals(city, StringComparison.OrdinalIgnoreCase));

            // Add new city at the beginning
            history.Insert(0, city);

            // Keep only last 5
            if (history.Count > 5)
            {
                history = history.Take(5).ToList();
            }

            // Store back in session as JSON using System.Text.Json
            var json = JsonSerializer.Serialize(history);
            HttpContext.Session.SetString(SEARCH_HISTORY_KEY, json);
        }

        /// <summary>
        /// Retrieves search history from Session
        /// </summary>
        private List<string> GetSearchHistory()
        {
            var historyJson = HttpContext.Session.GetString(SEARCH_HISTORY_KEY);

            if (string.IsNullOrEmpty(historyJson))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(historyJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets current temperature unit from Session
        /// </summary>
        private string GetTemperatureUnit()
        {
            return HttpContext.Session.GetString(TEMP_UNIT_KEY) ?? "Celsius";
        }

        /// <summary>
        /// Converts Celsius to Fahrenheit
        /// </summary>
        private double CelsiusToFahrenheit(double celsius)
        {
            return (celsius * 9 / 5) + 32;
        }

        /// <summary>
        /// Converts Fahrenheit to Celsius
        /// </summary>
        private double FahrenheitToCelsius(double fahrenheit)
        {
            return (fahrenheit - 32) * 5 / 9;
        }

        /// <summary>
        /// Helper method to deep clone objects using System.Text.Json
        /// </summary>
        private T DeepClone<T>(T obj)
        {
            if (obj == null) return default;

            // Use System.Text.Json for serialization (matching your WeatherService)
            var json = JsonSerializer.Serialize(obj);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}