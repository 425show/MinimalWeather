﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace WebApiWeather.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("{latitude},{longitude}")]
        public async Task<CombinedWeather> Get(double latitude, double longitude)
        {
            var currentQuery = _weatherService.GetFromJsonAsync<CurrentWeather>("currentConditions/json", $"&query={latitude},{longitude}");
            var hourlyQuery = _weatherService.GetFromJsonAsync<HourlyForecast>("forecast/hourly/json", $"&query={latitude},{longitude}&duration=24");
            var dailyQuery = _weatherService.GetFromJsonAsync<DailyForecast>("forecast/daily/json", $"&query={latitude},{longitude}&duration=10");

            // Wait for the 3 parallel requests to complete and combine the responses.
            return new()
            {
                CurrentWeather = (await currentQuery).Results[0],
                HourlyForecasts = (await hourlyQuery).Forecasts,
                DailyForecasts = (await dailyQuery).Forecasts
            };
        }
    }
}
