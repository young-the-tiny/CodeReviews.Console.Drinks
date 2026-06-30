# Unit Testing Tutorial for the Drinks Console App

This tutorial guide walks through writing comprehensive, robust unit and integration tests for the C# **Drinks** Console Application.

Unit testing ensures that as we add features, change libraries, or refactor our code, the core logic of our application remains correct and does not regress.

---

## Architecture for Testability

Before writing tests, let's look at why our codebase is easy to test:
1. **Interface Separation (`IDrinksApi`)**: The UI interacts with the API through an interface. This allows us to mock the API when testing the UI, preventing actual network calls during test execution.
2. **Dependency Injection**: `UserInput` accepts `IDrinksApi` and `DrinksDatabase` via its constructor. We can inject mock or test double implementations into the UI logic.
3. **Database Parametrization**: `DrinksDatabase` accepts a connection string path (defaulting to `"drinks.db"`). By passing `":memory:"`, we can run SQLite entirely in-memory, ensuring fast, isolated, and side-effect-free database tests.

---

## Step 0 — Setup the Test Project

We will use **xUnit** as our testing framework, **Moq** for mocking dependencies, and **FluentAssertions** for writing readable assertions.

Create a sibling directory for tests or add it to the solution. In your terminal, run:

```bash
# Create a new xUnit test project
dotnet new xunit -o DrinksInfo.Tests

# Add reference to the main project
dotnet add DrinksInfo.Tests/DrinksInfo.Tests.csproj reference DrinksInfo/DrinksInfo.csproj

# Add Moq for mocking dependencies
dotnet add DrinksInfo.Tests/DrinksInfo.Tests.csproj package Moq

# Add FluentAssertions for easier assertion reading (optional but highly recommended)
dotnet add DrinksInfo.Tests/DrinksInfo.Tests.csproj package FluentAssertions
```

Your `DrinksInfo.Tests.csproj` should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\DrinksInfo\DrinksInfo.csproj" />
  </ItemGroup>
</Project>
```

---

## Step 1 — Testing the Models (`DrinkDetail.Ingredients()`)

The `DrinkDetail.Ingredients()` method is pure, deterministic logic: it takes a dictionary of raw JSON fields and maps them into an enumerable list of ingredients and measurements. This is a perfect candidate for unit testing.

Create `ModelTests.cs` in the `DrinksInfo.Tests` project:

```csharp
using System.Text.Json;
using DrinksInfo;
using FluentAssertions;
using Xunit;

namespace DrinksInfo.Tests;

public class ModelTests
{
    [Fact]
    public void Ingredients_ShouldFilterNullAndEmptyValues_AndTrimWhitespace()
    {
        // Arrange: Build a raw JSON payload with varying cases of ingredients and measures
        var json = """
        {
            "idDrink": "11000",
            "strDrink": "Mojito",
            "strInstructions": "Muddle mint...",
            "strIngredient1": "Light rum",
            "strMeasure1": "2-3 oz",
            "strIngredient2": "  Lime  ",
            "strMeasure2": " Juice of 1 ",
            "strIngredient3": "",
            "strMeasure3": "1 tsp",
            "strIngredient4": null,
            "strMeasure4": null
        }
        """;
        
        var drinkDetail = JsonSerializer.Deserialize<DrinkDetail>(json);
        
        // Act
        var ingredients = drinkDetail!.Ingredients().ToList();

        // Assert
        // We expect only 2 items since Ingredient3 is empty and Ingredient4 is null
        ingredients.Should().HaveCount(2);
        
        // Verify whitespace trimming
        ingredients[0].Ingredient.Should().Be("Light rum");
        ingredients[0].Measure.Should().Be("2-3 oz");
        
        ingredients[1].Ingredient.Should().Be("Lime");
        ingredients[1].Measure.Should().Be("Juice of 1");
    }

    [Fact]
    public void Ingredients_WithNoIngredients_ShouldReturnEmptyList()
    {
        // Arrange
        var json = """
        {
            "idDrink": "11000",
            "strDrink": "Empty Drink"
        }
        """;
        
        var drinkDetail = JsonSerializer.Deserialize<DrinkDetail>(json);

        // Act
        var ingredients = drinkDetail!.Ingredients().ToList();

        // Assert
        ingredients.Should().BeEmpty();
    }
}
```

---

## Step 2 — Testing the Database (`DrinksDatabase`) with In-Memory SQLite

Because `DrinksDatabase` runs on SQLite, we don't need a heavy database server to run database tests. We can pass a special connection string `":memory:"` (or `Data Source=:memory:`) to create a database that lives only in RAM and is discarded when the connection closes.

Since each test needs an isolated, clean state, we will open a new connection for each test. We can refactor `DrinksDatabase` slightly or inherit from it to swap the connection method, or simply construct it with `":memory:"` for tests.

Create `DatabaseTests.cs` in the `DrinksInfo.Tests` project:

```csharp
using DrinksInfo;
using FluentAssertions;
using Xunit;

namespace DrinksInfo.Tests;

public class DatabaseTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DrinksDatabase _db;

    public DatabaseTests()
    {
        // Using a unique file name per test run ensures complete isolation.
        // SQLite in-memory can also be used if we share a single connection,
        // but a temporary file name is easier to work with Dapper's connection lifecycle.
        _dbPath = $"test_{Guid.NewGuid()}.db";
        _db = new DrinksDatabase(_dbPath);
    }

    public void Dispose()
    {
        // Cleanup the file database after each test
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch
            {
                // Ignore lock errors
            }
        }
    }

    [Fact]
    public void Constructor_ShouldInitializeSchema()
    {
        // Act & Assert: Schema initialization runs inside constructor. We verify by calling methods.
        var favorites = _db.GetFavorites();
        favorites.Should().BeEmpty();
    }

    [Fact]
    public void ToggleFavorite_ShouldAddAndRemoveCorrectly()
    {
        // Arrange
        var id = "11000";
        var name = "Mojito";

        // Act & Assert 1: Add to favorites
        var isFavAfterAdd = _db.ToggleFavorite(id, name);
        isFavAfterAdd.Should().BeTrue();
        _db.IsFavorite(id).Should().BeTrue();

        var favorites = _db.GetFavorites();
        favorites.Should().ContainSingle();
        favorites[0].DrinkId.Should().Be(id);
        favorites[0].DrinkName.Should().Be(name);

        // Act & Assert 2: Remove from favorites
        var isFavAfterRemove = _db.ToggleFavorite(id, name);
        isFavAfterRemove.Should().BeFalse();
        _db.IsFavorite(id).Should().BeFalse();
        _db.GetFavorites().Should().BeEmpty();
    }

    [Fact]
    public void IncrementView_ShouldInsertOrUpdateViews()
    {
        // Arrange
        var id = "11000";
        var name = "Mojito";

        // Act: First View
        _db.IncrementView(id, name);
        _db.GetViewCount(id).Should().Be(1);

        // Act: Second View
        _db.IncrementView(id, name);
        _db.GetViewCount(id).Should().Be(2);
    }

    [Fact]
    public void GetMostViewed_ShouldReturnDrinksOrderedByCount()
    {
        // Arrange
        _db.IncrementView("1", "Drink A");
        _db.IncrementView("1", "Drink A");
        _db.IncrementView("1", "Drink A"); // 3 views

        _db.IncrementView("2", "Drink B"); // 1 view

        _db.IncrementView("3", "Drink C");
        _db.IncrementView("3", "Drink C"); // 2 views

        // Act
        var topDrinks = _db.GetMostViewed(2); // Limit to top 2

        // Assert
        topDrinks.Should().HaveCount(2);
        topDrinks[0].DrinkName.Should().Be("Drink A");
        topDrinks[0].Count.Should().Be(3);
        
        topDrinks[1].DrinkName.Should().Be("Drink C");
        topDrinks[1].Count.Should().Be(2);
    }
}
```

---

## Step 3 — Mocking the HTTP Client (`HttpClientDrinksApi`)

Testing `HttpClientDrinksApi` directly would call the real Cocktails API. This makes tests slow, flaky, and dependent on internet access. 

To test our API client in isolation, we mock the underlying `HttpMessageHandler` inside `HttpClient`. We can define a mock class or use a helper class to intercept calls.

Create `HttpClientTests.cs` in the `DrinksInfo.Tests` project:

```csharp
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
}
```

### Refactoring `HttpClientDrinksApi` for Testability

Open `DrinksServices.cs` in your main project and modify `HttpClientDrinksApi` so the `HttpClient` can be injected for testing:

```csharp
public class HttpClientDrinksApi : IDrinksApi
{
    private static readonly HttpClient DefaultHttp = new()
    {
        BaseAddress = new Uri("https://www.thecocktaildb.com/api/json/v1/1/")
    };

    private readonly HttpClient _http;

    // Use default client in production, inject mock client in tests
    public HttpClientDrinksApi(HttpClient? http = null)
    {
        _http = http ?? DefaultHttp;
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var res = await _http.GetFromJsonAsync<CategoryList>("list.php?c=list");
        return res?.Drinks ?? new();
    }

    public async Task<List<Drink>> GetDrinksByCategoryAsync(string category)
    {
        var res = await _http.GetFromJsonAsync<DrinkList>(
            $"filter.php?c={Uri.EscapeDataString(category)}");
        return res?.Drinks ?? new();
    }

    public async Task<DrinkDetail?> GetDrinkByIdAsync(string id)
    {
        var res = await _http.GetFromJsonAsync<DrinkDetailList>($"lookup.php?i={id}");
        return res?.Drinks?.FirstOrDefault();
    }
}
```

Now we can write the test cases in `HttpClientTests.cs`:

```csharp
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
```

---

## Step 4 — Testing/Mocking the RestSharp Client (`RestSharpDrinksApi`)

Similar to the `HttpClient` implementation, we need to inject `RestClient` into `RestSharpDrinksApi` so we can test it using a mock client or a mock handler.

### Refactoring `RestSharpDrinksApi` for Testability

Modify `RestSharpDrinksApi` in `DrinksServices.cs` to allow injection of a `RestClient`:

```csharp
public class RestSharpDrinksApi : IDrinksApi
{
    private static readonly RestClient DefaultClient = new("https://www.thecocktaildb.com/api/json/v1/1/");
    private readonly RestClient _client;

    public RestSharpDrinksApi(RestClient? client = null)
    {
        _client = client ?? DefaultClient;
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var res = await _client.GetAsync<CategoryList>(new RestRequest("list.php?c=list"));
        return res?.Drinks ?? new();
    }

    public async Task<List<Drink>> GetDrinksByCategoryAsync(string category)
    {
        var req = new RestRequest("filter.php").AddQueryParameter("c", category);
        var res = await _client.GetAsync<DrinkList>(req);
        return res?.Drinks ?? new();
    }

    public async Task<DrinkDetail?> GetDrinkByIdAsync(string id)
    {
        var req = new RestRequest("lookup.php").AddQueryParameter("i", id);
        var res = await _client.GetAsync<DrinkDetailList>(req);
        return res?.Drinks?.FirstOrDefault();
    }
}
```

Now create `RestSharpTests.cs` in the `DrinksInfo.Tests` project. We can configure `RestClient` with a custom `HttpMessageHandler` just like we did with `HttpClient` to intercept and mock the underlying network request:

```csharp
using System.Net;
using DrinksInfo;
using FluentAssertions;
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
```

---

## Step 5 — How to Run Your Tests

To run the unit tests, open your command-line interface in the repository directory and execute:

```bash
dotnet test
```

This command automatically builds both the main project and the test project, runs the test runner, and outputs the results to your terminal.

Example output:

```
Passed!  - Failed:     0, Passed:     8, Skipped:     0, Total:     8, Duration: 232 ms - DrinksInfo.Tests.dll (net10.0)
```

---

## Testing Best Practices

1. **Keep Tests Independent**: Never let one unit test rely on the output of another test. Always use unique identifiers or clear database states per test.
2. **Use Clear Naming Conventions**: Name tests after the pattern: `MethodName_StateUnderTest_ExpectedBehavior`.
3. **Assert One logical Concept Per Test**: Make sure each test verifies a single functional expectation.
4. **Mock External IO**: Never allow unit tests to perform real network requests or read/write production databases. Mocking prevents tests from breaking due to network failures.
