using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrinksInfo;

public class CategoryList    { [JsonPropertyName("drinks")] public List<Category>? Drinks { get; set; } }
public class DrinkList       { [JsonPropertyName("drinks")] public List<Drink>? Drinks { get; set; } }
public class DrinkDetailList { [JsonPropertyName("drinks")] public List<DrinkDetail>? Drinks { get; set; } }

public class Category
{
    [JsonPropertyName("strCategory")] public string Name { get; set; } = "";
    public override string ToString() => Name;
}

public class Drink
{
    [JsonPropertyName("idDrink")]       public string Id { get; set; } = "";
    [JsonPropertyName("strDrink")]      public string Name { get; set; } = "";
    [JsonPropertyName("strDrinkThumb")] public string? Thumbnail { get; set; }
    public override string ToString() => Name;
}

public class DrinkDetail
{
    [JsonPropertyName("idDrink")]         public string Id { get; set; } = "";
    [JsonPropertyName("strDrink")]        public string Name { get; set; } = "";
    [JsonPropertyName("strCategory")]     public string? Category { get; set; }
    [JsonPropertyName("strInstructions")] public string? Instructions { get; set; }
    [JsonPropertyName("strDrinkThumb")]   public string? Thumbnail { get; set; }

    [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; } = new();

    public IEnumerable<(string Ingredient, string Measure)> Ingredients()
    {
        for (int i = 1; i <= 15; i++)
        {
            var ing = Str($"strIngredient{i}");
            if (string.IsNullOrWhiteSpace(ing)) continue;
            yield return (ing.Trim(), (Str($"strMeasure{i}") ?? "").Trim());
        }
    }

    private string? Str(string key) =>
        Extra.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}