using CoffeeMachineAPI.Model;
using System.Text.Json;

namespace CoffeeMachineAPI.Service
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public WeatherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<WeatherResponse> GetCurrentWeatherAsync(double lat, double lon)
        {
            var apiUrl = _configuration["ExternalApi:Url"];
            var apiKey = _configuration["ExternalApi:ApiKey"];

            var response = await _httpClient.GetAsync($"{apiUrl}?lat={lat}&lon={lon}&units=metric&appid={apiKey}");
            var weatherData = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());

            double temp = weatherData.GetProperty("main").GetProperty("temp").GetDouble();

            return new WeatherResponse
            {
                StatusCode = 200,
                Temperature = temp
            };
        }
    }

}