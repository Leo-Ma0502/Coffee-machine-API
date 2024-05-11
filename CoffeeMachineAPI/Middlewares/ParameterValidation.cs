using System.Text.Json;
using CoffeeMachineAPI.Model;

namespace CoffeeMachineAPI.Middleware
{
    public class ParameterValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ParameterValidationMiddleware> _logger;

        public ParameterValidationMiddleware(RequestDelegate next, ILogger<ParameterValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // validate the parameter (lat, lon) for endpoint "/brew-coffee" 
            // the parameters are required for weather service
            // any issues related to weather service is not expected to interrupt the coffee brew service
            // so just warning will be given
            if (context.Request.Path.StartsWithSegments("/brew-coffee"))
            {
                var latQuery = context.Request.Query["lat"];
                var lonQuery = context.Request.Query["lon"];
                // exclude the situation of null and not number
                double? lat = latQuery.Count > 0 ? double.TryParse(latQuery[0], out var parsedLat) ? parsedLat : (double?)null : null;
                double? lon = lonQuery.Count > 0 ? double.TryParse(lonQuery[0], out var parsedLon) ? parsedLon : (double?)null : null;

                Location? location;

                if (lat == null || lon == null)
                {
                    _logger.LogWarning("Both 'lat' and 'lon' parameters are required and must be valid numbers for this endpoint.");
                    location = null;
                }
                else if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                {
                    _logger.LogWarning("Parameters 'lat' must be between -90 and 90 and 'lon' must be between -180 and 180.");
                    location = null;
                }
                else
                {
                    location = new Location { Lat = lat, Lon = lon };
                }

                // set the context item
                context.Items["Location"] = location;
            }

            await _next(context);
        }
    }

    public static class ParameterValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseParameterValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ParameterValidationMiddleware>();
        }
    }
}
