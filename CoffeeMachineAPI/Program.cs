using CoffeeMachineAPI.Middleware;
using CoffeeMachineAPI.Model;
using CoffeeMachineAPI.Service;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
builder.Services.AddSingleton<ICoffeeService, CoffeeService>();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseParameterValidation();
app.UseExceptionHandling();

app.MapGet("/brew-coffee", async (HttpContext http, ICoffeeService coffeeService, [FromQuery] string? lat, [FromQuery] string? lon) =>
{
    // the updated endpint takes latitude and longitude in the params
    // get location from the middleware
    Location? location = http.Items.TryGetValue("Location", out var locationObj) ? locationObj as Location : null;

    // the updated BrewCoffee method takes the location as parameter, as required by the third-party API
    var response = await coffeeService.BrewCoffee(location);
    http.Response.StatusCode = response.StatusCode;
    http.Response.ContentType = "application/json";
    if (response.Body != null) { await http.Response.WriteAsync(response.Body); }
});

app.Run();

