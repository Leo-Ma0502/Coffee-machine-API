using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using CoffeeMachineAPI.Service;
using CoffeeMachineAPI.Model;

public class WeatherServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly WeatherService _weatherService;

    public WeatherServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(c => c["ExternalApi:Url"]).Returns("http://test.com");
        _mockConfiguration.Setup(c => c["ExternalApi:ApiKey"]).Returns("testapikey");

        _weatherService = new WeatherService(_httpClient, _mockConfiguration.Object);
    }

    // Only normal case here, the weather service is not expected to throw exceptions

    [Fact]
    public async Task GetCurrentWeatherAsync_ReturnsWeatherResponseWithTemperature()
    {
        var jsonResponse = "{\"main\":{\"temp\":25.5}}";
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var result = await _weatherService.GetCurrentWeatherAsync(35.6895, 139.6917);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(25.5, result.Temperature);
    }
}
