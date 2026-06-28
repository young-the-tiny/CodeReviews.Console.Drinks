using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrinksInfo;

public class CategoryList { [JsonPropertyName("drink")] public List<Category>? Drinks { get; set; } };
public class DrinksList { [JsonPropertyName("drink")] public List<Drink>? Drinks { get; set; } };
public class DrinksDetailList { [JsonPropertyName("drinks")] public List<DrinkDetail>? Drinks { get; set; } };

public class Category
{
    [JsonPropertyName("strCategory")] 
    public string? Name { get; set; }

    public override string ToString() => Name;
}

public class Drink
{
    [JsonPropertyName("idDrink")] 
    public string? Id { get; set; }
    [JsonPropertyName("strDrink")] 
    public string? Name { get; set; }
    [JsonPropertyName("strDrinkThumb")] 
    public string? ImageUrl { get; set; }

    public override string ToString() => Name;
}
public class DrinkDetail
{
    [JsonPropertyName("idDrink")] 
    public string? Id { get; set; }
    [JsonPropertyName("strDrink")] 
    public string? Name { get; set; }
    [JsonPropertyName("strCategory")] 
    public string? Category { get; set; }
    [JsonPropertyName("strInstructions")] 
    public string? Instructions { get; set; }
    [JsonPropertyName("strDrinkThumb")] 
    public string? ImageUrl { get; set; }

    // Ingredients and Measures
    [JsonExtensionData] 
    public Dictionary<string, JsonElement> ExtenstionData { get; set; } = new();
    public IEnumerable<(string Ingredient, string Measure)> Ingredients()
    {
        for (int i = 1; i<=15; i++)
        {
            var ing = Str($"strIngredients{i}");
            if (string.IsNullOrWhiteSpace(ing)) continue;
            yield return (ing.Trim(), (Str($"strMeasure{i}") ?? "").Trim());
        }
    }
    private string? Str(string k) =>
        ExtenstionData.TryGetValue(k, out var val) && val.ValueKind == JsonValueKind.String // GetString() never throw
            ? val.GetString()
            : null;
}