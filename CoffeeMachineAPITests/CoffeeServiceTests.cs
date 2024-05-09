using System;
using Moq;
using Xunit;
using CoffeeMachineAPI.Service;
using CoffeeMachineAPI.Model;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using CoffeeMachineAPI.Middleware;

public class CoffeeServiceTests
{
    private readonly Mock<IDateTimeService> _mockDateTimeService;
    private readonly CoffeeService _coffeeService;

    public CoffeeServiceTests()
    {
        _mockDateTimeService = new Mock<IDateTimeService>();
        _coffeeService = new CoffeeService(_mockDateTimeService.Object);
    }

    /*
    * normal cases
    **/

    // normal situation
    [Fact]
    public void BrewCoffee_RegularDay_ReturnsStatusCode200AndMessage()
    {
        var now = new DateTime(2030, 5, 2, 14, 0, 1);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        var response = _coffeeService.BrewCoffee();

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("Your piping hot coffee is ready", response.Body);
    }

    // April 1st -> 418
    [Fact]
    public void BrewCoffee_OnApril1st_ReturnsStatusCode418AndNoMessage()
    {
        var aprilFools = new DateTime(2033, 4, 1);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(aprilFools);

        var response = _coffeeService.BrewCoffee();

        Assert.Equal(418, response.StatusCode);
        Assert.Null(response.Body);
    }

    // 5*n request -> 503
    [Fact]
    public void BrewCoffee_EveryFifthRequest_ReturnsStatusCode503AndNoMessage()
    {
        var now = new DateTime(2026, 4, 20);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        for (int i = 0; i < 4; i++)
        {
            _coffeeService.BrewCoffee();
        }

        var response = _coffeeService.BrewCoffee();

        Assert.Equal(503, response.StatusCode);
        Assert.Null(response.Body);
    }

    /*
    * edge cases
    **/

    // concurrent requests
    [Fact]
    public async Task BrewCoffee_202ConcurrentRequests_ShouldTrigger503EveryFifthRequest()
    {
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(s => s.GetCurrentTime()).Returns(DateTime.Now);
        var coffeeService = new CoffeeService(dateTimeService.Object);

        int numberOfRequests = 202;
        var tasks = new List<Task<CoffeeResponse>>();

        for (int i = 0; i < numberOfRequests; i++)
        {
            tasks.Add(Task.Run(() => coffeeService.BrewCoffee()));
        }

        var responses = await Task.WhenAll(tasks);

        var serviceUnavailableResponses = responses.Count(response => response.StatusCode == 503);

        Assert.True(serviceUnavailableResponses == 40, "Expected 202/5 503 responses.");
    }

    // near April 1st
    [Theory]
    [InlineData("2023-04-01T23:59:59")]
    [InlineData("2023-04-02T00:00:01")]
    public void BrewCoffee_TimeEdgeCases_ShouldHandleCorrectly(string dateTime)
    {
        var edgeDateTime = DateTime.Parse(dateTime);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(edgeDateTime);

        var response = _coffeeService.BrewCoffee();

        Assert.Equal(edgeDateTime.Day == 1 && edgeDateTime.Month == 4 ? 418 : 200, response.StatusCode);
    }

    // extreme numbers of requests
    [Fact]
    public void BrewCoffee_ExtremeRequestVolume_ShouldNotOverflowOrCrash()
    {
        var now = new DateTime(2023, 5, 10);
        _mockDateTimeService.Setup(s => s.GetCurrentTime()).Returns(now);

        typeof(CoffeeService)
            .GetField("requestCount", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_coffeeService, int.MaxValue - 10);

        var response = _coffeeService.BrewCoffee();

        var expectedStatusCodes = new List<int> { 200, 503 };

        Assert.True(expectedStatusCodes.Contains(response.StatusCode), "Returned valid status code under extreme request numbers.");
    }

    // exception handling
    [Fact]
    public async Task InvokeAsync_ThrowsException_HandlesAndReturnsInternalServerErrorCode()
    {
        var mockRequestDelegate = new Mock<RequestDelegate>();
        var middleware = new ExceptionHandlingMiddleware(mockRequestDelegate.Object);
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

}