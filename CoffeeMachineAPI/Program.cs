using CoffeeMachineAPI.Middleware;
using CoffeeMachineAPI.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
builder.Services.AddSingleton<ICoffeeService, CoffeeService>();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseExceptionHandling();

app.MapGet("/brew-coffee", async (HttpContext http, ICoffeeService coffeeService) =>
{
    var response = await coffeeService.BrewCoffee();
    http.Response.StatusCode = response.StatusCode;
    http.Response.ContentType = "application/json";
    if (response.Body != null) { await http.Response.WriteAsync(response.Body); }
});

app.Run();

