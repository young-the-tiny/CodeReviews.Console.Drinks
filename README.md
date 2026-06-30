# Drinks Info — C# Console Application

An interactive C# console application for browsing drink recipes, categories, and ingredients. Built with .NET 10.0, the app connects directly to [TheCocktailDB API](https://www.thecocktaildb.com/) and provides rich visual interfaces directly in the terminal, including drink thumbnail renderings.

This project was built to compare two different HTTP request client styles (raw `.NET HttpClient` and `RestSharp`) behind a unified service interface, and features local data persistence using SQLite and Dapper.

---

## 🚀 Features

* **Rich Terminal UI:** Multi-level menus, tables, panels, and borders provided by `Spectre.Console`.
* **Console Image Rendering:** Displays drink thumbnail graphics directly in the terminal using `Spectre.Console.ImageSharp` (with built-in error handling and download timeouts to ensure a stable offline/slow-network experience).
* **Interchangeable API Implementations:** Choose between raw `.NET HttpClient` and `RestSharp` at launch.
* **Local Persistence:** Automatically tracks view counts and stores user-selected favorite drinks in a local SQLite database (`drinks.db`) using raw SQL queries mapped via `Dapper`.
* **Robust Test Suite:** Unit and integration tests written using `xUnit`, `FluentAssertions`, and `Moq`.

---

## 🛠️ Tech Stack

* **Language/Runtime:** C# / .NET 10.0
* **API Clients:** `HttpClient` (built-in) and `RestSharp`
* **Data Mapping (Micro-ORM):** `Dapper`
* **Database Engine:** `SQLite` (`Microsoft.Data.Sqlite`)
* **Terminal Formatting & Graphics:** `Spectre.Console` & `Spectre.Console.ImageSharp`
* **Testing:** `xUnit`, `Moq`, and `FluentAssertions`

---

## 📂 Project Structure

```
├── DrinksInfo
│   ├── Database.cs          # SQLite + Dapper operations (upserts & queries)
│   ├── DrinksServices.cs    # IDrinksApi interface & implementations (HttpClient / RestSharp)
│   ├── Models.cs            # API models & extension data deserialization helpers
│   ├── UserInput.cs         # Spectre menus, recipe screens, and terminal flows
│   ├── Program.cs           # Application entry point & configuration wiring
│   └── DrinksInfo.Tests     # Unit & integration test suites
```

---

## 💾 Database Schema

The app uses two simple tables stored in a local SQLite database (`drinks.db`):

### 1. `Favorites`
* `Id` (TEXT PRIMARY KEY) - The identifier of the drink.
* `Name` (TEXT NOT NULL) - The name of the drink.

### 2. `Views`
* `Id` (TEXT PRIMARY KEY) - The identifier of the drink.
* `Name` (TEXT NOT NULL) - The name of the drink.
* `Count` (INTEGER NOT NULL) - View count incremented on select (utilizes SQLite's `ON CONFLICT` upsert syntax).

---

## ⚙️ Getting Started

### Prerequisites

Make sure you have the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) installed on your system.

### Build and Run

1. Navigate to the project root directory.
2. Build the project:
   ```bash
   dotnet build
   ```
3. Run the application:
   ```bash
   dotnet run --project DrinksInfo
   ```

### Running Tests

Run the test suite (covering database logic, custom model conversions, and integration/mock HTTP behaviors):
```bash
dotnet test
```
