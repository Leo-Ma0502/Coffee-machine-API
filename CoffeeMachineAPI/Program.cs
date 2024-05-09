using CoffeeMachineAPI.Middleware;
using CoffeeMachineAPI.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
builder.Services.AddSingleton<ICoffeeService, CoffeeService>();

var app = builder.Build();

app.UseExceptionHandling();

app.MapGet("/brew-coffee", (HttpContext http, ICoffeeService coffeeService) =>
{
    var response = coffeeService.BrewCoffee();
    http.Response.StatusCode = response.StatusCode;
    http.Response.ContentType = "application/json";
    if (response.Body != null) { http.Response.WriteAsync(response.Body); }
});

app.Run();

