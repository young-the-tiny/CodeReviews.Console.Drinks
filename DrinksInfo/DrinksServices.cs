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
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://www.thecocktaildb.com/api/json/v1/1/")
    };

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var res = await Http.GetFromJsonAsync<CategoryList>("list.php?c=list");
        return res?.Drinks ?? new();
    }

    public async Task<List<Drink>> GetDrinksByCategoryAsync(string category)
    {
        var res = await Http.GetFromJsonAsync<DrinkList>(
            $"filter.php?c={Uri.EscapeDataString(category)}");
        return res?.Drinks ?? new();
    }

    public async Task<DrinkDetail?> GetDrinkByIdAsync(string id)
    {
        var res = await Http.GetFromJsonAsync<DrinkDetailList>($"lookup.php?i={id}");
        return res?.Drinks?.FirstOrDefault();
    }
}

public class RestSharpDrinksApi : IDrinksApi
{
    private static readonly RestClient Client = new("https://www.thecocktaildb.com/api/json/v1/1/");

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var res = await Client.GetAsync<CategoryList>(new RestRequest("list.php?c=list"));
        return res?.Drinks ?? new();
    }

    public async Task<List<Drink>> GetDrinksByCategoryAsync(string category)
    {
        var req = new RestRequest("filter.php").AddQueryParameter("c", category);
        var res = await Client.GetAsync<DrinkList>(req);
        return res?.Drinks ?? new();
    }

    public async Task<DrinkDetail?> GetDrinkByIdAsync(string id)
    {
        var req = new RestRequest("lookup.php").AddQueryParameter("i", id);
        var res = await Client.GetAsync<DrinkDetailList>(req);
        return res?.Drinks?.FirstOrDefault();
    }
}