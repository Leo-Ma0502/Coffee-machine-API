using System.Text.Json;
using CoffeeMachineAPI.Model;
using CoffeeMachineAPI.Service;

public class CoffeeService : ICoffeeService
{
    private IDateTimeService _dateTimeService;
    private int requestCount = 0;

    public CoffeeService(IDateTimeService dateTimeService)
    {
        _dateTimeService = dateTimeService;
    }

    public CoffeeResponse BrewCoffee()
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

        return new CoffeeResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new { Message = "Your piping hot coffee is ready", Prepared = prepared })
        };
    }
}
