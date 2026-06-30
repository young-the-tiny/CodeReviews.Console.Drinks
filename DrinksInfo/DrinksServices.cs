using System.Net.Http.Json;
using RestSharp;

namespace DrinksInfo;

public interface IDrinksApi
{
    Task<List<Category>> GetCategoriesAsync();
    Task<List<Drink>> GetDrinksByCategoryAsync(string category);
    Task<DrinkDetail?> GetDrinkByIdAsync(string id);
}

public class HttpClientDrinksApi : IDrinksApi
{
    private readonly HttpClient _http;
    private static readonly HttpClient DefaultHttp = new()
    {
        BaseAddress = new Uri("https://www.thecocktaildb.com/api/json/v1/1/")
    };

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

public class RestSharpDrinksApi : IDrinksApi
{
    private readonly RestClient _client;
    private static readonly RestClient DefaultClient = new("https://www.thecocktaildb.com/api/json/v1/1/");

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