using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace DotNetCoreRedis.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IDistributedCache _distributedCache;
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger,
            IDistributedCache distributedCache)
    {
        _logger = logger;
        _distributedCache = distributedCache;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        // Redis üzerinden verileri al
        var cacheKey = "WeatherForecastList";
        var serializedData = await _distributedCache.GetStringAsync(cacheKey);

        // Eğer veri Redis'te mevcutsa, çöz ve dön
        if (serializedData != null)
        {
            return JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(serializedData);
        }
        else // Veri Redis'te mevcut değilse, yeniden oluştur ve Redis'e kaydet
        {
            var weatherForecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();

            serializedData = JsonConvert.SerializeObject(weatherForecasts);
            var options = new DistributedCacheEntryOptions()
                                .SetAbsoluteExpiration(DateTime.Now.AddMinutes(10))
                                .SetSlidingExpiration(TimeSpan.FromMinutes(2));
            await _distributedCache.SetStringAsync(cacheKey, serializedData, options);

            return weatherForecasts;
        }
    }

}
