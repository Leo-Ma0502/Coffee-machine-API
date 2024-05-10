using CoffeeMachineAPI.Middleware;
using CoffeeMachineAPI.Model;
using CoffeeMachineAPI.Service;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
builder.Services.AddSingleton<ICoffeeService, CoffeeService>();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseExceptionHandling();

app.MapGet("/brew-coffee", async (HttpContext http, ICoffeeService coffeeService, [FromQuery] double? lat, [FromQuery] double? lon) =>
{
    var location = new Location
    {
        Lat = lat,
        Lon = lon
    };
    var response = await coffeeService.BrewCoffee(location);
    http.Response.StatusCode = response.StatusCode;
    http.Response.ContentType = "application/json";
    if (response.Body != null) { await http.Response.WriteAsync(response.Body); }
});

app.Run();

