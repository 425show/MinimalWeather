# Weather Samples

## Minimal ASP.NET Core

### [Program.cs](https://github.com/halter73/MinimalWeather/blob/main/dotnet/MinimalWeather/Program.cs)

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalWeather;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("weather", policyBuilder => policyBuilder.AllowAnyOrigin());
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

var baseQuery = $"api-version=1.0&subscription-key={app.Configuration["SubscriptionKey"]}&unit=imperial";
using var httpClient = new HttpClient()
{
    BaseAddress = new Uri("https://atlas.microsoft.com/weather/")
};

app.UseCors();
app.UseHttpsRedirection();

app.MapGet("/weather/{location}", [EnableCors("weather")] async (Coordinate location) =>
{
    var currentQuery = httpClient.GetFromJsonAsync<CurrentWeather>($"currentConditions/json?{baseQuery}&query={location}");
    var hourlyQuery = httpClient.GetFromJsonAsync<HourlyForecast>($"forecast/hourly/json?{baseQuery}&query={location}&duration=24");
    var dailyQuery = httpClient.GetFromJsonAsync<DailyForecast>($"forecast/daily/json?{baseQuery}&query={location}&duration=10");

    // Wait for the 3 parallel requests to complete and combine the responses.
    return new
    {
        CurrentWeather = (await currentQuery).Results[0],
        HourlyForecasts = (await hourlyQuery).Forecasts,
        DailyForecasts = (await dailyQuery).Forecasts
    };
});

app.Run();
```

## ASP.NET Core Web API

### [Program.cs](https://github.com/halter73/MinimalWeather/blob/main/dotnet/WebApiWeather/Program.cs)

```csharp

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApiWeather
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
```

### [Startup.cs](https://github.com/halter73/MinimalWeather/blob/main/dotnet/WebApiWeather/Startup.cs)

```csharp

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace WebApiWeather
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddCors(options =>
            {
                options.AddPolicy("weather", policyBuilder => policyBuilder.AllowAnyOrigin());
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApiWeather", Version = "v1" });
            });

            services.AddSingleton<WeatherService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApiWeather v1"));
            }

            app.UseRouting();

            app.UseCors();
            app.UseHttpsRedirection();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
```

### [WeatherService.cs](https://github.com/halter73/MinimalWeather/blob/main/dotnet/WebApiWeather/WeatherService.cs)

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace WebApiWeather
{
    public class WeatherService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseQueryString;

        public WeatherService(IConfiguration config)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://atlas.microsoft.com/weather/")
            };

            _baseQueryString = $"api-version=1.0&subscription-key={config["SubscriptionKey"]}&unit=imperial";
        }

        public Task<T> GetFromJsonAsync<T>(string path, string extraQuery)
             => _httpClient.GetFromJsonAsync<T>($"{path}?{_baseQueryString}{extraQuery}");

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
```

### [WeatherController.cs](https://github.com/halter73/MinimalWeather/blob/main/dotnet/WebApiWeather/Controllers/WeatherController.cs)

```csharp
using System.Threading.Tasks;
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
        [EnableCors("weather")]
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
© 2021 GitHub, Inc.
```

## Express

### [app.ts](https://github.com/halter73/MinimalWeather/blob/main/node/ExpressWeather/app.ts)

```typescript
import * as cors from 'cors';
import * as express from 'express';
import got from 'got';

const app = express();

var baseUrl = "https://atlas.microsoft.com/weather/";
var baseQuery = `api-version=1.0&subscription-key=${process.env["SubscriptionKey"]}&unit=imperial`;

app.get('/weather/:lat,:lon', cors(), async (req, res, next) => {
    try {
        const lat = parseFloat(req.params.lat);
        const lon = parseFloat(req.params.lon);

        const currentQuery = got(`${baseUrl}currentConditions/json?${baseQuery}&query=${lat},${lon}`);
        const hourlyQuery = got(`${baseUrl}forecast/hourly/json?${baseQuery}&query=${lat},${lon}&duration=24`);
        const dailyQuery = got(`${baseUrl}forecast/daily/json?${baseQuery}&query=${lat},${lon}&duration=10`);

        // Wait for the 3 parallel requests to complete and combine the responses.
        const [currentResponse, hourlyResponse, dailyResponse] = await Promise.all([currentQuery, hourlyQuery, dailyQuery]);

        const currentWeather = JSON.parse(currentResponse.body);
        const hourlyForecast = JSON.parse(hourlyResponse.body);
        const dailyForecast = JSON.parse(dailyResponse.body);

        await res.json({
            currentWeather: currentWeather.results[0],
            hourlyForecasts: hourlyForecast.forecasts,
            dailyForecasts: dailyForecast.forecasts,
        });
    } catch (err) {
        // Express doesn't handle async errors natively yet.
        next(err);
    }
});

const port = process.env.PORT || 3000

app.listen(port, function () {
    console.log(`Express server listening on port ${port}`);
});
```
