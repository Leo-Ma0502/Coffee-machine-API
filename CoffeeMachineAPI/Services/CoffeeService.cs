using System.Text.Json;
using CoffeeMachineAPI.Exceptions;
using CoffeeMachineAPI.Model;
using CoffeeMachineAPI.Service;
using Microsoft.AspNetCore.Mvc;

public class CoffeeService : ICoffeeService
{
    private IDateTimeService _dateTimeService;
    private IWeatherService _weatherService;
    private int requestCount = 0;

    public CoffeeService(IDateTimeService dateTimeService, IWeatherService weatherService)
    {
        _dateTimeService = dateTimeService;
        _weatherService = weatherService;
    }

    public async Task<CoffeeResponse> BrewCoffee([FromBody] Location? location)
    {
        var now = _dateTimeService.GetCurrentTime();
        var prepared = $"{now.ToString("yyyy-MM-ddTHH:mm:ss")}{now.ToString("zzz").Replace(":", "")}";

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

        try
        {
            var temperatureData = await _weatherService.GetCurrentWeatherAsync(location?.Lat, location?.Lon);
            var message = temperatureData?.Temperature > 30 ? "Your refreshing iced coffee is ready" : "Your piping hot coffee is ready";

            return new CoffeeResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new
                {
                    Message = message,
                    Prepared = prepared,
                })
            };
        }
        catch (Exception e)
        {
            // produce a CoffeeResponse even if there is an exception
            // but including error information
            var protectedResponse = new CoffeeResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new
                {
                    Message = "Your piping hot coffee is ready",
                    Prepared = prepared,
                    ErrorOfWeatherService = e.Message
                })
            };
            // throw the above information, including the coffee response that should be returned and the error details
            throw new WeatherServiceException(JsonSerializer.Serialize(protectedResponse));
        }
    }
}
