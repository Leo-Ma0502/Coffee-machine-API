using CoffeeMachineAPI.Model;

namespace CoffeeMachineAPI.Service
{
    public interface IWeatherService
    {
        Task<WeatherResponse> GetCurrentWeatherAsync(double lat, double lon);
    }
}