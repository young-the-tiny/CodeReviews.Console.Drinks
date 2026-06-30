using System.Text.Json;
using DrinksInfo;
using FluentAssertions;
using Xunit;

namespace DrinksInfo.Tests;

public class ModelsTest
{
    [Fact]
    public void Ingredient_FilterNullAndTrimWhiteSpace()
    {
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

        //act
        var ingredients = drinkDetail!.Ingredients().ToList();
        //assert
        ingredients.Should().HaveCount(2);
        ingredients[0].Ingredient.Should().Be("Light rum");
        ingredients[0].Measure.Should().Be("2-3 oz");

        ingredients[1].Ingredient.Should().Be("Lime");
        ingredients[1].Measure.Should().Be("Juice of 1");
    }
    [Fact]
    public void Ingredient_ReturnEmptyList()
    {
        var json = """
        {
            "idDrink": "11000",
            "strDrink": "Empty Drink"
        }
        """;
        var drinkDetail = JsonSerializer.Deserialize<DrinkDetail>(json);

        //act
        var ingredients = drinkDetail!.Ingredients().ToList();
        //assert
        ingredients.Should().BeEmpty();
    }
    [Fact]
    public async Task TestRealApi_HttpClient()
    {
        var api = new HttpClientDrinksApi();
        var categories = await api.GetCategoriesAsync();
        categories.Should().NotBeEmpty();
        
        var firstCategory = categories[0].Name;
        var drinks = await api.GetDrinksByCategoryAsync(firstCategory);
        drinks.Should().NotBeEmpty();
        
        var firstDrink = drinks[0].Id;
        var details = await api.GetDrinkByIdAsync(firstDrink);
        details.Should().NotBeNull();
    }

    [Fact]
    public async Task TestRealApi_RestSharp()
    {
        var api = new RestSharpDrinksApi();
        var categories = await api.GetCategoriesAsync();
        categories.Should().NotBeEmpty();
        
        var firstCategory = categories[0].Name;
        var drinks = await api.GetDrinksByCategoryAsync(firstCategory);
        drinks.Should().NotBeEmpty();
        
        var firstDrink = drinks[0].Id;
        var details = await api.GetDrinkByIdAsync(firstDrink);
        details.Should().NotBeNull();
    }
}

