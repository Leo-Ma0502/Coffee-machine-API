using System.Text.Json;
using CoffeeMachineAPI.Model;
using CoffeeMachineAPI.Service;
using Microsoft.AspNetCore.Mvc;

public class CoffeeService : ICoffeeService
{
    private IDateTimeService _dateTimeService;
    private IWeatherService _weatherService;
    private IHttpContextAccessor _httpContextAccessor;
    private int requestCount = 0;

    public CoffeeService(IDateTimeService dateTimeService, IWeatherService weatherService, IHttpContextAccessor httpContextAccessor)
    {
        _dateTimeService = dateTimeService;
        _weatherService = weatherService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CoffeeResponse> BrewCoffee([FromBody] Location? location)
    {
        var now = _dateTimeService.GetCurrentTime();
        var prepared = now.ToString("yyyy-MM-ddTHH:mm:sszzz");

        if (now.Month == 4 && now.Day == 1)
        {
            return new CoffeeResponse
            {
                StatusCode = 418
            };
        }

        Interlocked.Increment(ref requestCount);
        if (requestCount % 5 == 0)
        {
            return new CoffeeResponse
            {
                StatusCode = 503,
            };
        }

        WeatherResponse temperatureData;
        try { temperatureData = await _weatherService.GetCurrentWeatherAsync(location?.Lat, location?.Lon); }
        catch { temperatureData = (WeatherResponse)_httpContextAccessor.HttpContext.Items["DefaultWeatherData"]; }

        return new CoffeeResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new
            {
                Message = temperatureData?.Temperature > 30 ? "Your refreshing iced coffee is ready" : "Your piping hot coffee is ready",
                Prepared = prepared
            })
        };
    }
}
