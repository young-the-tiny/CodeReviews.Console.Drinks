using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using DrinksInfo;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace DrinksInfo.Tests;

public class HttpClientTests
{
    // A helper method to create an HttpClient backed by a mocked HttpMessageHandler
    private HttpClient CreateMockHttpClient(HttpResponseMessage responseMessage)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage)
            .Verifiable();

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://www.thecocktaildb.com/api/json/v1/1/")
        };
    }

    // Since our HttpClientDrinksApi uses a static private HttpClient, 
    // it is hard to swap out for tests.
    // For unit testing, we should refactor HttpClientDrinksApi slightly to accept
    // the HttpClient via constructor injection, falling back to a static instance.
    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnCategories_WhenApiRespondsSuccessfully()
    {
        // Arrange
        var mockResponseJson = """
        {
            "drinks": [
                { "strCategory": "Ordinary Drink" },
                { "strCategory": "Cocktail" }
            ]
        }
        """;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockResponseJson, System.Text.Encoding.UTF8, "application/json")
        };

        var mockClient = CreateMockHttpClient(httpResponse);
        var apiService = new HttpClientDrinksApi(mockClient);

        // Act
        var result = await apiService.GetCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Ordinary Drink");
        result[1].Name.Should().Be("Cocktail");
    }

    [Fact]
    public async Task GetDrinkByIdAsync_ShouldReturnNull_WhenDrinkNotFound()
    {
        // Arrange
        var mockResponseJson = """
        {
            "drinks": null
        }
        """;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockResponseJson, System.Text.Encoding.UTF8, "application/json")
        };

        var mockClient = CreateMockHttpClient(httpResponse);
        var apiService = new HttpClientDrinksApi(mockClient);

        // Act
        var result = await apiService.GetDrinkByIdAsync("99999");

        // Assert
        result.Should().BeNull();
        }
    }
}