using DrinksInfo;
using FluentAssertions;
using Xunit;

namespace DrinksInfo.Tests;

public class DatabaseTests : IDisposable
{
    private readonly string _dbPath;
    private readonly Database _database;

    public DatabaseTests()
    {
        // Using a unique file name per test run ensures complete isolation.
        // SQLite in-memory can also be used if we share a single connection,
        // but a temporary file name is easier to work with Dapper's connection lifecycle.
        _dbPath = $"test_{Guid.NewGuid()}.db";
        _database = new Database(_dbPath);
    }
    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch
            { }
        }
    }
    [Fact]
    public void Construct_InitializeSchema()
    {
        // Act & assert
        var favorites = _database.GetFavoriteDrinks();
        favorites.Should().BeEmpty();
    }
    [Fact]
    public void ToggleFavoriteShouldUpdateAndDelete()
    {
        var id = "1000";
        var name = "Coca";
        // Act and assert: Add to favorite
        var isFavAfterAdd = _database.ToggleFavorite(id, name);
        isFavAfterAdd.Should().BeTrue();
        _database.isFavorite(id).Should().BeTrue();

        var favorites = _database.GetFavoriteDrinks();
        favorites.Should().ContainSingle();
        favorites[0].Id.Should().Be(id);
        favorites[0].Name.Should().Be(name);

        // act and assert 2: Remove from favorites
        var isFavRemoved = _database.ToggleFavorite(id, name);
        isFavRemoved.Should().BeFalse();
        _database.isFavorite(id).Should().BeFalse();
        _database.GetFavoriteDrinks().Should().BeEmpty();
    }
    [Fact]
    public void IncrementView_ShouldInsertOrUpdate()
    {
        //arrange
        var id = "1001";
        var name = "Coca Cola";

        // Act and assert: View 1
        _database.IncrementView(id, name);
        _database.GetViewCount(id).Should().Be(1);

        // Act and assert: View 2
        _database.IncrementView(id, name);
        _database.GetViewCount(id).Should().Be(2);
    }
    [Fact]
    public void GetMostView_OrderByCount()
    {
        // Arrange
        _database.IncrementView("1", "Drink A");
        _database.IncrementView("1", "Drink A");
        _database.IncrementView("1", "Drink A"); // 3 views

        _database.IncrementView("2", "Drink B"); // 1 view

        _database.IncrementView("3", "Drink C");
        _database.IncrementView("3", "Drink C"); // 2 views

        var topDrink = _database.GetMostView(2);

        // Assert
        topDrink.Should().NotBeNull();
        topDrink.Should().HaveCount(2);
        topDrink[0].Name.Should().Be("Drink A");
        topDrink[0].count.Should().Be(3);

        topDrink[1].Name.Should().Be("Drink C");
        topDrink[1].count.Should().Be(2);
    }
}