using System.Text.Json;
using CoffeeMachineAPI.Exceptions;
using CoffeeMachineAPI.Model;

namespace CoffeeMachineAPI.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                await HandleExceptionAsync(context, e);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception e)
        {
            if (e is WeatherServiceException)
            {
                var messageFromCoffeeService = JsonDocument.Parse(e.Message).RootElement;
                // body of messageFromCoffeeService, containing Message, Prepared, ErrorOfWeatherService
                var messageBody = JsonDocument.Parse(messageFromCoffeeService.GetProperty("Body").ToString()).RootElement;
                // remove the error message from the response body
                var responseBody = new { Message = messageBody.GetProperty("Message"), Prepared = messageBody.GetProperty("Prepared") };
                // add error details in the warning
                _logger.LogWarning($"Weather service error, ignore the temperature.\nError details:\n{messageBody.GetProperty("ErrorOfWeatherService")}");
                await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody));
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { Error = $"An error occurred while processing your request:\n{e}" });
            }
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}