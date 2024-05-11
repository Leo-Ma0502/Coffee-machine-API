using System;
using Moq;
using Xunit;
using CoffeeMachineAPI.Service;
using CoffeeMachineAPI.Model;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using CoffeeMachineAPI.Middleware;
using Microsoft.Extensions.Logging;
using CoffeeMachineAPI.Exceptions;
using System.Text.Json.Nodes;
using System.Text.Json;

public class CoffeeServiceTests
{
    private readonly Mock<IDateTimeService> _mockDateTimeService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly CoffeeService _coffeeService;

    public CoffeeServiceTests()
    {
        _mockDateTimeService = new Mock<IDateTimeService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _coffeeService = new CoffeeService(_mockDateTimeService.Object, _mockWeatherService.Object);
    }

    /*
    * normal cases
    **/

    // normal situation
    [Fact]
    public async void BrewCoffee_RegularDay_ReturnsStatusCode200AndMessage()
    {
        var now = new DateTime(2030, 5, 2, 14, 0, 1);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);
        var response = await _coffeeService.BrewCoffee(null);

        var responseBody = JsonDocument.Parse(response.Body?.ToString()).RootElement;
        var Message = responseBody.GetProperty("Message").ToString();
        var Prepared = responseBody.GetProperty("Prepared").ToString();

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("Your piping hot coffee is ready", Message);
        Assert.Equal($"{now.ToString("yyyy-MM-ddTHH:mm:ss")}{now.ToString("zzz").Replace(":", "")}", Prepared);
    }

    // April 1st -> 418
    [Fact]
    public async void BrewCoffee_OnApril1st_ReturnsStatusCode418AndNoMessage()
    {
        var aprilFools = new DateTime(2033, 4, 1);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(aprilFools);
        var response = await _coffeeService.BrewCoffee(null);

        Assert.Equal(418, response.StatusCode);
        Assert.Null(response.Body);
    }

    // 5*n request -> 503
    [Fact]
    public async void BrewCoffee_EveryFifthRequest_ReturnsStatusCode503AndNoMessage()
    {
        var now = new DateTime(2026, 4, 20);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        for (int i = 0; i < 4; i++)
        {
            await _coffeeService.BrewCoffee(null);
        }

        var response = await _coffeeService.BrewCoffee(null);

        Assert.Equal(503, response.StatusCode);
        Assert.Null(response.Body);
    }

    // temperature > 30 -> iced coffee
    [Fact]
    public async void BrewCoffee_TemperatureAbove30C_ReturnsStatusCode300AndIcedCoffeeMessage()
    {
        var now = new DateTime(2026, 4, 20);
        var temperatureData = new WeatherResponse { StatusCode = 200, Temperature = 35 };
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync(null, null)).Returns(Task.FromResult(temperatureData));
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        var response = await _coffeeService.BrewCoffee(null);

        var responseBody = JsonDocument.Parse(response.Body?.ToString()).RootElement;
        var Message = responseBody.GetProperty("Message").ToString();
        var Prepared = responseBody.GetProperty("Prepared").ToString();

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("Your refreshing iced coffee is ready", Message);
        Assert.Equal($"{now.ToString("yyyy-MM-ddTHH:mm:ss")}{now.ToString("zzz").Replace(":", "")}", Prepared);
    }


    /*
    * edge cases
    **/

    // concurrent requests
    [Fact]
    public async Task BrewCoffee_202ConcurrentRequests_ShouldTrigger503EveryFifthRequest()
    {
        var now = new DateTime(2026, 4, 20);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        int numberOfRequests = 202;
        var tasks = new List<Task<CoffeeResponse>>();

        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(Task.Run(() => _coffeeService.BrewCoffee(null)));

        }

        var responses = await Task.WhenAll(tasks);

        var serviceUnavailableResponses = responses.Count(response => response.StatusCode == 503);

        Assert.True(serviceUnavailableResponses == 40, "Expected 202/5 503 responses.");
    }

    // concurrent requests on April 1st -> all 418
    [Fact]
    public async Task BrewCoffee_202ConcurrentRequests_AprilFoolsDay_ShouldReturn418EveryRequest()
    {
        var now = new DateTime(2026, 4, 1);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        int numberOfRequests = 402;
        var tasks = new List<Task<CoffeeResponse>>();

        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(Task.Run(() => _coffeeService.BrewCoffee(null)));
        }

        var responses = await Task.WhenAll(tasks);

        var serviceUnavailableResponses = responses.Count(response => response.StatusCode == 418);


        Assert.True(serviceUnavailableResponses == 402, $"Expected 402 418 responses, actual: {serviceUnavailableResponses}");
    }

    // near April 1st
    [Theory]
    [InlineData("2023-04-01T23:59:59")]
    [InlineData("2023-04-02T00:00:01")]
    public async void BrewCoffee_TimeEdgeCases_ShouldHandleCorrectly(string dateTime)
    {
        var edgeDateTime = DateTime.Parse(dateTime);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(edgeDateTime);

        var response = await _coffeeService.BrewCoffee(null);

        Assert.Equal(edgeDateTime.Day == 1 && edgeDateTime.Month == 4 ? 418 : 200, response.StatusCode);
    }

    // extreme numbers of requests
    [Fact]
    public async void BrewCoffee_ExtremeRequestVolume_ShouldNotOverflowOrCrash()
    {
        var now = new DateTime(2023, 5, 10);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        typeof(CoffeeService)
            .GetField("requestCount", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_coffeeService, int.MaxValue - 10);

        var response = await _coffeeService.BrewCoffee(null);

        var expectedStatusCodes = new List<int> { 200, 503 };

        Assert.True(expectedStatusCodes.Contains(response.StatusCode), "Returned valid status code under extreme request numbers.");
    }

    // general exception handling
    [Fact]
    public async Task InvokeAsync_ThrowsException_HandlesAndReturnsInternalServerErrorCode()
    {
        var mockRequestDelegate = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(mockRequestDelegate.Object, mockLogger.Object);
        var context = new DefaultHttpContext();

        context.Response.Body = new System.IO.MemoryStream();

        mockRequestDelegate.Setup(r => r(It.IsAny<HttpContext>())).Throws(new Exception("Mock exception"));

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        var reader = new System.IO.StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains("Mock exception", responseBody);
    }

    // WeatherService exception handling: 
    // The CoffeeService is expected to throw WeatherServiceException when WeatherService throws any exception
    [Fact]
    public async Task HandleExceptionAsync_ThrowsWeatherServiceException()
    {
        var mockRequestDelegate = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(mockRequestDelegate.Object, mockLogger.Object);

        var now = new DateTime(2026, 4, 20);
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync(null, null))
                          .ThrowsAsync(new Exception("Any exception"));
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        var exception = await Assert.ThrowsAsync<WeatherServiceException>(() => _coffeeService.BrewCoffee(null));
        // exception message
        var eMessage = JsonDocument.Parse(exception.Message).RootElement;
        var StatusCode = eMessage.GetProperty("StatusCode");
        var Body = JsonDocument.Parse(eMessage.GetProperty("Body").ToString()).RootElement;

        // {
        //   “Message”: “Your piping hot coffee is ready”,
        //   “Prepared”: “2021-02- 03T11:56:24+0900”，
        //   “ErrorOfWeatherService” = e.Message
        // };
        var Message = Body.GetProperty("Message");
        var Prepared = Body.GetProperty("Prepared");
        var ErrorOfWeatherService = Body.GetProperty("ErrorOfWeatherService");

        // the throwed error should contain coffee response and error details
        Assert.Equal(200, Int32.Parse(StatusCode.ToString()));
        Assert.Equal("Your piping hot coffee is ready", Message.ToString());
        Assert.Equal($"{now.ToString("yyyy-MM-ddTHH:mm:ss")}{now.ToString("zzz").Replace(":", "")}", Prepared.ToString());
        Assert.Equal("Any exception", ErrorOfWeatherService.ToString());
    }

    // WeatherService exception handling: 
    // The pipeline is expected to handle WeatherServiceException
    [Fact]
    public async Task HandleExceptionAsync_LogsWarningAndReturnsIngoringWeather()
    {
        var mockRequestDelegate = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();

        var now = new DateTime(2026, 4, 20);

        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync(null, null))
                          .ThrowsAsync(new Exception("Any exception"));
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        var middleware = new ExceptionHandlingMiddleware(mockRequestDelegate.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        mockRequestDelegate.Setup(rd => rd.Invoke(It.IsAny<HttpContext>()))
                           .ThrowsAsync(new WeatherServiceException(JsonSerializer.Serialize(new CoffeeResponse
                           {
                               StatusCode = 200,
                               Body = JsonSerializer.Serialize(new
                               {
                                   Message = "Your piping hot coffee is ready",
                                   Prepared = $"{now.ToString("yyyy-MM-ddTHH:mm:ss")}{now.ToString("zzz").Replace(":", "")}",
                                   ErrorOfWeatherService = "Any exception"
                               })
                           })));

        await middleware.InvokeAsync(context);

        // Verify logging
        mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Weather service error, ignore the temperature.\nError details:\nAny exception")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);

        // Verify status code
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        // Verify response content
        context.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        var reader = new System.IO.StreamReader(context.Response.Body);
        var responseBody = JsonDocument.Parse(await reader.ReadToEndAsync()).RootElement;
        var Message = responseBody.GetProperty("Message");
        var Prepared = responseBody.GetProperty("Prepared");
        Assert.Equal("Your piping hot coffee is ready", Message.ToString());
        Assert.Equal($"{now.ToString("yyyy-MM-ddTHH:mm:ss")}{now.ToString("zzz").Replace(":", "")}", Prepared.ToString());
    }

    // WeatherService exception handling on April 1st: 
    // The pipeline is expected to return 418 directly
    [Fact]
    public async Task BrewCoffee_WeatherServiceExceptionOnApril1st_ReturnsStatusCode418AndNoMessage()
    {
        var mockRequestDelegate = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();

        var now = new DateTime(2036, 4, 1);
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync(null, null))
                          .ThrowsAsync(new Exception("Any exception"));
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        var response = await _coffeeService.BrewCoffee(null);

        Assert.Equal(418, response.StatusCode);
        Assert.Null(response.Body);
    }

    // Request with no location parameter -> return normally, with warning
    [Fact]
    public async Task BrewCoffee_NoLocationParameter_ReturnsStatusCode200WithWarning()
    {
        var now = new DateTime(2024, 5, 10);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        var temperatureData = new WeatherResponse { StatusCode = 200, Temperature = 20 };
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync(null, null)).Returns(Task.FromResult(temperatureData));

        var mockRequestDelegate = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<ParameterValidationMiddleware>>();
        var middleware = new ParameterValidationMiddleware(mockRequestDelegate.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/brew-coffee";
        context.Request.QueryString = new QueryString("?lat=&lon=");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        Location? location = context.Items.TryGetValue("Location", out var locationObj) ? locationObj as Location : null;
        var response = await _coffeeService.BrewCoffee(null);

        var responseBody = JsonDocument.Parse(response.Body?.ToString()).RootElement;
        var Message = responseBody.GetProperty("Message").ToString();
        var Prepared = responseBody.GetProperty("Prepared").ToString();

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("Your piping hot coffee is ready", Message);
        Assert.Equal($"{now.ToString("yyyy-MM-ddTHH:mm:ss")}{now.ToString("zzz").Replace(":", "")}", Prepared);

        mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Both 'lat' and 'lon' parameters are required and must be valid numbers for this endpoint.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    // Request with invalid location parameters -> return normally, with warning
    [Fact]
    public async Task BrewCoffee_InvalidLocationParameter_ReturnsStatusCode200WithWarning()
    {
        var now = new DateTime(2024, 5, 10);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        var temperatureData = new WeatherResponse { StatusCode = 200, Temperature = 20 };
        _mockWeatherService.Setup(s => s.GetCurrentWeatherAsync(null, null)).Returns(Task.FromResult(temperatureData));

        var mockRequestDelegate = new Mock<RequestDelegate>();
        var mockLogger = new Mock<ILogger<ParameterValidationMiddleware>>();
        var middleware = new ParameterValidationMiddleware(mockRequestDelegate.Object, mockLogger.Object);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/brew-coffee";
        context.Request.QueryString = new QueryString("?lat=9999&lon=9999");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Location? location = context.Items.TryGetValue("Location", out var locationObj) ? locationObj as Location : null;
        var response = await _coffeeService.BrewCoffee(location);

        var responseBody = JsonDocument.Parse(response.Body?.ToString()).RootElement;
        var Message = responseBody.GetProperty("Message").ToString();
        var Prepared = responseBody.GetProperty("Prepared").ToString();

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("Your piping hot coffee is ready", Message);
        Assert.Equal($"{now.ToString("yyyy-MM-ddTHH:mm:ss")}{now.ToString("zzz").Replace(":", "")}", Prepared);

        mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Parameters 'lat' must be between -90 and 90 and 'lon' must be between -180 and 180.")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}
