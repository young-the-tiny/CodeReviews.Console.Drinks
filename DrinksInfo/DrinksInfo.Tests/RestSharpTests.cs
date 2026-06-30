using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DrinksInfo;
using FluentAssertions;
using Moq;
using Moq.Protected;
using RestSharp;
using Xunit;

namespace DrinksInfo.Tests;

public class RestSharpTests
{
    private RestClient CreateMockRestClient(HttpResponseMessage mockResponse)
    {

        // RestClient can accept custom RestClientOptions containing a custom HttpMessageHandler
        var handlerMock = new Moq.Mock<HttpMessageHandler>(Moq.MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(mockResponse);

        var options = new RestClientOptions("https://www.thecocktaildb.com/api/json/v1/1/")
        {
            ConfigureMessageHandler = _ => handlerMock.Object
        };

        return new RestClient(options);
    }

    [Fact]
    public async Task GetDrinksByCategoryAsync_ShouldReturnDrinks_WhenApiRespondsSuccessfully()
    {
        // Arrange
        var mockResponseJson = """
        {
            "drinks": [
                { "idDrink": "11000", "strDrink": "Mojito", "strDrinkThumb": "thumb.jpg" }
            ]
        }
        """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockResponseJson, System.Text.Encoding.UTF8, "application/json")
        };

        var mockClient = CreateMockRestClient(response);
        var apiService = new RestSharpDrinksApi(mockClient);

        // Act
        var result = await apiService.GetDrinksByCategoryAsync("Cocktail");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("11000");
        result[0].Name.Should().Be("Mojito");
    }
}