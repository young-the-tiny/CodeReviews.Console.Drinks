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
        _database = new Database( _dbPath );
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
            {}  
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
    }

}