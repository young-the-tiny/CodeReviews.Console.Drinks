using Dapper;
using Microsoft.Data.Sqlite;

namespace DrinksInfo;

public record favoriteDrink(string Id, string Name);
public record viewRow(string Name, int count);

public class Database
{
    private readonly string _connStr;
    public Database(string path = "drinks.db")
    {
        _connStr = $"Data Source = {path}";
        using var c = Open();
        // Runs once at startup; "IF NOT EXISTS" makes it safe
        c.Execute("""
            CREATE TABLE IF NOT EXISTS Favorites (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Views (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Count INTEGER NOT NULL DEFAULT 0);
            """);
    } 
    private SqliteConnection Open() => new(_connStr);
    public void IncrementView(string id, string name)
    {
        using var c = Open();
        c.Execute("""
            INSERT INTO Views (Id, Name, Count) 
            VALUES (@id, @name, 1)
            ON CONFLICT(Id) DO UPDATE SET Count = Count + 1;
            """, new { id, name });
    }
    public int GetViewCount(string id)
    {
        using var c = Open();
        return c.ExecuteScalar<int?>("SELECT Count FROM Views WHERE Id = @id", new { id }) ?? 0;
    }
    public List<viewRow> GetMostView(int top = 10)
    {
        using var c = Open();
        return c.Query<viewRow>("SELECT Name, Count FROM Views ORDER BY Count DESC LIMIT @top", new { top }).ToList();
    }
    public bool isFavorite(string id)
    {
        using var c = Open();
        return c.ExecuteScalar<int>("SELECT COUNT(1) FROM Favorites WHERE Id = @id", new { id }) > 0;
    }
    public bool ToggleFavorite(string id, string name)
    {
        using var c = Open();
        if (isFavorite(id))
        {
            c.Execute("DELETE FROM Favorites WHERE Id = @id", new { id });
            return false;
        }
        c.Execute("INSERT INTO Favorites (Id, Name) VALUES (@id, @name,)", new { id, name });
        return true;
    }
    public List<favoriteDrink> GetFavoriteDrinks()
    {
        using var c = Open();
        return c.Query<favoriteDrink>("SELECT * FROM Favorites ORDER BY Name").ToList();
    }
}