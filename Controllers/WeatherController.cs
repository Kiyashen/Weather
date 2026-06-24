using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Weather.Services;

namespace Weather.Controllers
{
    public class WeatherController : Controller
    {
        private readonly WeatherService _weatherService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        // Automatically injects your API service, along with Member 2's cache and configuration systems
        public WeatherController(WeatherService weatherService, IMemoryCache cache, IConfiguration configuration)
        {
            _weatherService = weatherService;
            _cache = cache;
            _configuration = configuration;
        }

        // GET: /Weather
        [HttpGet]
        public IActionResult Index()
        {
            // ==========================================
            // MEMBER 2: TASK - SESSION INITIALIZATION
            // ==========================================
            // Read temperature unit state toggle (Celsius vs Fahrenheit) from Session State.
            // Pass recent 5-city history list to ViewBag so Member 3 can display clickable quick-links.

            return View();
        }

        // GET: /Weather/Search?city=Durban
        [HttpGet]
        public async Task<IActionResult> Search(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                ViewBag.ErrorMessage = "Please enter a valid city name.";
                return View("Index");
            }

            string cleanCityKey = city.Trim().ToLower();

            // Reads dynamically from your existing setup: "WeatherApiSettings" -> "CacheExpiryMinutes"
            var expiryMinutes = _configuration.GetValue<int>("WeatherApiSettings:CacheExpiryMinutes", 30);

            // Create placeholders for our data objects
            Weather.Models.API.CurrentWeatherResponse currentData = null;
            Weather.Models.API.ForecastWeatherResponse forecastData = null;

            // ======================================================================
            // MEMBER 2: TASK - IMEMORYCACHE INTERCEPT LAYER
            // ======================================================================
            // 1. Check if 'cleanCityKey' data already exists inside _cache.
            // 2. If it EXISTS: Load it instantly from memory to save API requests.
            // 3. If it DOES NOT EXIST: Run the live API block below, then save the data into _cache using the expiryMinutes setting.

            // --- LIVE API CALL BLOCK ---
            currentData = await _weatherService.GetCurrentWeatherAsync(city);
            forecastData = await _weatherService.GetForecastWeatherAsync(city);
            // ---------------------------

            if (currentData == null || forecastData == null)
            {
                ViewBag.ErrorMessage = $"Could not retrieve weather data for '{city}'. Please check the spelling.";
                return View("Index");
            }

            // ======================================================================
            // MEMBER 2: TASK - SESSION ARRAY TRACKING (LAST 5 CITIES)
            // ======================================================================
            // 1. Retrieve the existing list of tracked cities from HttpContext.Session.
            // 2. Append the current city name to the collection array.
            // 3. Keep ONLY the last 5 unique searched cities in the key-value array.
            // 4. Save the updated list back into the active session block.

            // Package payloads up for the view
            ViewBag.Current = currentData;
            ViewBag.Forecast = forecastData;
            ViewBag.SearchedCity = city;

            return View("Index");
        }

        // POST/GET Action endpoint for Temperature Scale Toggle
        [HttpPost]
        public IActionResult ToggleUnits(string currentCity, string unit)
        {
            // ======================================================================
            // MEMBER 2: TASK - TEMPERATURE SCALE TOGGLE CALCULATIONS
            // ======================================================================
            // 1. Save chosen unit string ("C" or "F") into Session State tracking parameter.
            // 2. If a city search is currently active, load its data directly from the IMemoryCache layers.
            // 3. Execute conversion mathematical formulas purely on the backend server engine to convert values WITHOUT re-pinging the OpenWeatherMap API endpoints.

            return RedirectToAction("Search", new { city = currentCity });
        }
    }
}