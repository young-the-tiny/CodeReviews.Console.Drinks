# Drinks Info — C# Console Application

An interactive C# console application for browsing drink recipes, categories, and ingredients. Built with .NET 10.0, the app connects directly to [TheCocktailDB API](https://www.thecocktaildb.com/) and provides rich visual interfaces directly in the terminal, including drink thumbnail renderings.

This project was built to compare two different HTTP request client styles (raw `.NET HttpClient` and `RestSharp`) behind a unified service interface, and features local data persistence using SQLite and Dapper.

## Project Structure

```
├── DrinksInfo
│   ├── Database.cs          # SQLite + Dapper operations
│   ├── DrinksServices.cs    # IDrinksApi interface & implementations 
│   ├── Models.cs            # API models & extension data deserialization helpers
│   ├── UserInput.cs         # Spectre menus, recipe screens, and terminal flows
│   ├── Program.cs           # Application entry point & configuration wiring
│   └── DrinksInfo.Tests     # Testing file
```
---
## Database Schema

The app uses two tables stored in a local SQLite database (`drinks.db`):

### 1. `Favorites`
* `Id` (TEXT PRIMARY KEY) - The identifier of the drink.
* `Name` (TEXT NOT NULL) - The name of the drink.

### 2. `Views`
* `Id` (TEXT PRIMARY KEY) - The identifier of the drink.
* `Name` (TEXT NOT NULL) - The name of the drink.
* `Count` (INTEGER NOT NULL) - View count incremented on select (utilizes SQLite's `ON CONFLICT` upsert syntax).

---

### Running Tests

Run the test suite (covering database logic, custom model conversions, and integration/mock HTTP behaviors):
```bash
dotnet test
```
