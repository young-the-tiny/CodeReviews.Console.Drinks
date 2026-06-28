using System.Net.Http.Json;

namespace DrinksInfo;

public interface IDrinkApi
{
    Task<List<Category>> GetCategoriesAsync();
    Task<List<Drink>> GetDrinksByCategoryAsync(string category);
    Task<List<DrinkDetail>> GetDrinkDetailsAsync(string id);
}

public class HttpClients : IDrinkApi
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://www.thecocktaildb.com/api/json/v1/1/")
    };
    public async Task<List<Category>> GetCategoriesAsync()
    {
        // GetFromJsonAsync does GET + read body + deserialize in one call.
        var res = await Http.GetFromJsonAsync<CategoryList>("list.php?=list");
        return res?.Drinks ?? new();
    }
    public async Task<List<Drink>> GetDrinksByCategoryAsync(string category)
    {
        // EscapeDataString handles spaces, e.g. "Ordinary Drink" -> "Ordinary%20Drink".
        var res = await Http.GetFromJsonAsync<DrinksList>(
            $"filter.php?c{Uri.EscapeDataString(category)}");
        return res?.Drinks ?? new();
    }
    public async Task<DrinkDetail?> GetDrinkDetailsAsync(string id)
    {
        var res = await Http.GetFromJsonAsync<DrinksDetailList>($"lookup.php?i={id}");
        return res?.Drinks?.FirstOrDefault();
    }



}