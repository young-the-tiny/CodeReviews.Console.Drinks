# Drinks Console App — A Highly Detailed Hand-Coding Tutorial

## Context

You're building the C# Academy **Drinks** project from an empty `net10.0` console app.
The repo starts with only scaffolding: `Program.cs` ("Hello, World!") and three blank files
(`DrinksServices.cs`, `UserInput.cs`, `Validator.cs`).

By the end you'll have a **Spectre.Console** app that talks to **TheCocktailDB** and lets you:
browse categories → drinks → full recipe, see the drink's **picture in the console**, mark
**favorites**, and track a **view count** — all persisted in **SQLite**. The API layer has
**two interchangeable implementations** (raw `HttpClient` and `RestSharp`) so you can compare
the two styles side by side.

You are **typing all of this yourself**. Every step below gives you the code *and* explains
what each part does and why. Type it, don't paste — that's the point.

### Decisions locked in
- API: TheCocktailDB (`https://www.thecocktaildb.com/api/json/v1/1/`)
- Two impls behind `IDrinksApi`, chosen at launch; one project shares models/UI/storage
- Storage: SQLite via **Dapper + Microsoft.Data.Sqlite** (tables: Favorites, Views)
- UI: **Spectre.Console**; picture via **Spectre.Console.ImageSharp**
- REST client: **RestSharp**

### Final file layout
| File | Role | Step |
|------|------|------|
| `DrinksInfo.csproj` | packages | 0 |
| `Models.cs` (new) | API DTOs + ingredient helper | 1 |
| `DrinksServices.cs` | `IDrinksApi` + both implementations | 2–3 |
| `DrinksDatabase.cs` (new) | SQLite/Dapper: favorites + views | 4 |
| `UserInput.cs` | Spectre menus, flow, picture | 5 |
| `Program.cs` | startup wiring | 6 |
| `Validator.cs` | **delete it** — see the note in Step 5 | — |

---

## How TheCocktailDB works (read this first)

Three GET endpoints, all returning JSON shaped like `{ "drinks": [ ... ] }`:

1. **Categories** — `list.php?c=list`
   ```json
   { "drinks": [ { "strCategory": "Ordinary Drink" }, { "strCategory": "Cocktail" } ] }
   ```
2. **Drinks in a category** — `filter.php?c=Cocktail`
   ```json
   { "drinks": [ { "strDrink": "Mojito", "strDrinkThumb": "https://...jpg", "idDrink": "11000" } ] }
   ```
3. **One drink's full detail** — `lookup.php?i=11000`
   ```json
   { "drinks": [ {
       "idDrink": "11000", "strDrink": "Mojito", "strCategory": "Cocktail",
       "strInstructions": "Muddle mint...", "strDrinkThumb": "https://...jpg",
       "strIngredient1": "Light rum", "strMeasure1": "2-3 oz",
       "strIngredient2": "Lime",      "strMeasure2": "Juice of 1",
       "strIngredient3": null, "strMeasure3": null
       /* ...up to strIngredient15 / strMeasure15, mostly null */
   } ] }
   ```

The awkward part is #3: ingredients are **15 flat numbered fields** instead of an array, and
most are `null`. Step 1 handles that cleanly so the rest of the app sees a tidy list.

---

## Step 0 — Create packages

The project already exists. From the project folder, add the five packages:

```sh
dotnet add package Spectre.Console
dotnet add package Spectre.Console.ImageSharp
dotnet add package RestSharp
dotnet add package Dapper
dotnet add package Microsoft.Data.Sqlite
```

What each is for:
- **Spectre.Console** — the menus, tables, panels, colors.
- **Spectre.Console.ImageSharp** — adds `CanvasImage`, which renders a JPEG as colored
  blocks in the terminal.
- **RestSharp** — the second HTTP client (the comparison target).
- **Dapper** — a tiny "micro-ORM": you write SQL, it maps rows to objects in one line.
- **Microsoft.Data.Sqlite** — the actual SQLite driver Dapper runs on.

`HttpClient` and the JSON helpers need nothing extra — they're built into .NET.

Confirm it builds before writing any code:
```sh
dotnet build
```

---

## Step 1 — `Models.cs`: turn JSON into objects

Create a new file `Models.cs`. We define small classes that match the JSON, using
`[JsonPropertyName]` to bridge TheCocktailDB's `strThing` naming to clean C# names.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrinksInfo;

// Each endpoint wraps its payload in a "drinks" array — these three model that envelope.
public class CategoryList    { [JsonPropertyName("drinks")] public List<Category>? Drinks { get; set; } }
public class DrinkList       { [JsonPropertyName("drinks")] public List<Drink>? Drinks { get; set; } }
public class DrinkDetailList { [JsonPropertyName("drinks")] public List<DrinkDetail>? Drinks { get; set; } }

public class Category
{
    [JsonPropertyName("strCategory")] public string Name { get; set; } = "";
    public override string ToString() => Name;   // Spectre shows this in the menu
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

    // Any JSON property we did NOT map above lands here. That's all the
    // strIngredient1..15 / strMeasure1..15 fields — without 30 hand-written properties.
    [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; } = new();

    // Turns the 15 flat fields into a clean list, skipping the null/empty ones.
    public IEnumerable<(string Ingredient, string Measure)> Ingredients()
    {
        for (int i = 1; i <= 15; i++)
        {
            var ing = Str($"strIngredient{i}");
            if (string.IsNullOrWhiteSpace(ing)) continue;          // skip empty slots
            yield return (ing.Trim(), (Str($"strMeasure{i}") ?? "").Trim());
        }
    }

    // Safely read one extension field as a string (it may be missing or JSON null).
    private string? Str(string key) =>
        Extra.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
```

**Why it's built this way**
- `[JsonPropertyName("strDrink")]` lets the JSON keep its ugly names while your code reads
  `drink.Name`. The deserializer matches on the attribute, not the C# property name.
- `ToString()` override → when you hand a `Category`/`Drink` to a Spectre `SelectionPrompt`,
  it prints the name automatically. No extra mapping.
- `[JsonExtensionData]` is the trick that avoids writing `StrIngredient1 … StrIngredient15`
  and `StrMeasure1 … StrMeasure15`. Everything unmapped is dumped into `Extra`, and
  `Ingredients()` walks 1→15 pulling out the pairs that actually have a value.
- `JsonElement` is the raw parsed value; `ValueKind == JsonValueKind.String` guards against
  the `null` slots so `GetString()` never throws.

You verify this method works in Step 7 with a tiny self-check.

---

## Step 2 — `DrinksServices.cs`: the interface + the HttpClient version

Open the existing `DrinksServices.cs` (currently blank). First the **contract** every API
version must fulfill, then the first implementation.

```csharp
using System.Net.Http.Json;
using RestSharp;

namespace DrinksInfo;

// The three operations the UI needs. The UI depends ONLY on this interface,
// so it never knows or cares which HTTP library is behind it.
public interface IDrinksApi
{
    Task<List<Category>> GetCategoriesAsync();
    Task<List<Drink>> GetDrinksByCategoryAsync(string category);
    Task<DrinkDetail?> GetDrinkByIdAsync(string id);
}

// --- Version A: raw HttpClient + the built-in System.Net.Http.Json helpers ---
public class HttpClientDrinksApi : IDrinksApi
{
    // One shared client. BaseAddress is the common prefix; each call adds the rest.
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://www.thecocktaildb.com/api/json/v1/1/")
    };

    public async Task<List<Category>> GetCategoriesAsync()
    {
        // GetFromJsonAsync does GET + read body + deserialize in one call.
        var res = await Http.GetFromJsonAsync<CategoryList>("list.php?c=list");
        return res?.Drinks ?? new();        // null-safe: empty list if anything's missing
    }

    public async Task<List<Drink>> GetDrinksByCategoryAsync(string category)
    {
        // EscapeDataString handles spaces, e.g. "Ordinary Drink" -> "Ordinary%20Drink".
        var res = await Http.GetFromJsonAsync<DrinkList>(
            $"filter.php?c={Uri.EscapeDataString(category)}");
        return res?.Drinks ?? new();
    }

    public async Task<DrinkDetail?> GetDrinkByIdAsync(string id)
    {
        var res = await Http.GetFromJsonAsync<DrinkDetailList>($"lookup.php?i={id}");
        return res?.Drinks?.FirstOrDefault();   // the array always has 0 or 1 item here
    }
}
```

**Why it's built this way**
- **`static readonly HttpClient`** — one instance reused for the whole app. Creating a new
  `HttpClient` per call is the classic .NET bug (it leaks sockets). One shared client is the
  correct pattern for a small app like this.
- **`BaseAddress`** — set the common prefix once; each method only writes the endpoint part.
- **`GetFromJsonAsync<T>`** — built into .NET (`System.Net.Http.Json`). It's GET +
  status check + JSON deserialize collapsed into a single line. No `JsonSerializer` call,
  no manual stream reading.
- **`res?.Drinks ?? new()`** — if the response or its `Drinks` is null, hand back an empty
  list so the UI never has to null-check. Defensive at the boundary where bad data enters.

---

## Step 3 — `DrinksServices.cs` (cont.): the RestSharp version

Add the second implementation to the **same file**, below the first one. It fulfils the exact
same `IDrinksApi`, so the rest of the app can't tell them apart — that's what makes the
comparison fair.

```csharp
// --- Version B: RestSharp. Its default serializer is System.Text.Json, so the same
//     [JsonPropertyName] attributes on your models just work. ---
public class RestSharpDrinksApi : IDrinksApi
{
    private static readonly RestClient Client =
        new("https://www.thecocktaildb.com/api/json/v1/1/");

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var res = await Client.GetAsync<CategoryList>(new RestRequest("list.php?c=list"));
        return res?.Drinks ?? new();
    }

    public async Task<List<Drink>> GetDrinksByCategoryAsync(string category)
    {
        // RestSharp builds the query string for you and escapes the value.
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
```

**HttpClient vs RestSharp — what to notice when comparing**
| | HttpClient (A) | RestSharp (B) |
|---|---|---|
| Package | built in | extra NuGet |
| One-call GET+deserialize | `GetFromJsonAsync<T>(url)` | `GetAsync<T>(RestRequest)` |
| Query params | you build the string + escape | `.AddQueryParameter(k, v)` |
| Serializer | System.Text.Json | System.Text.Json (default) |
| Feel | lean, closer to the metal | fluent request builder |

Both return identical objects. The point of the exercise: see that a clean interface lets you
swap the entire networking layer without touching the UI.

---

## Step 4 — `DrinksDatabase.cs`: SQLite + Dapper

Create `DrinksDatabase.cs`. Two tiny tables: `Favorites` and `Views`. Dapper turns each query
into one line.

```csharp
using Dapper;
using Microsoft.Data.Sqlite;

namespace DrinksInfo;

// Small record types Dapper maps rows onto (column name -> property name).
public record FavoriteRow(string DrinkId, string DrinkName);
public record ViewRow(string DrinkName, int Count);

public class DrinksDatabase
{
    private readonly string _connStr;

    public DrinksDatabase(string path = "drinks.db")
    {
        _connStr = $"Data Source={path}";   // SQLite = just a file on disk
        using var c = Open();
        // Runs once at startup; "IF NOT EXISTS" makes it safe to run every launch.
        c.Execute("""
            CREATE TABLE IF NOT EXISTS Favorites (
                DrinkId   TEXT PRIMARY KEY,
                DrinkName TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Views (
                DrinkId   TEXT PRIMARY KEY,
                DrinkName TEXT NOT NULL,
                Count     INTEGER NOT NULL DEFAULT 0);
            """);
    }

    private SqliteConnection Open() => new(_connStr);

    // "Upsert": first view inserts with Count=1; later views bump the existing row.
    public void IncrementView(string id, string name)
    {
        using var c = Open();
        c.Execute("""
            INSERT INTO Views (DrinkId, DrinkName, Count) VALUES (@id, @name, 1)
            ON CONFLICT(DrinkId) DO UPDATE SET Count = Count + 1;
            """, new { id, name });
    }

    public int GetViewCount(string id)
    {
        using var c = Open();
        return c.ExecuteScalar<int?>(
            "SELECT Count FROM Views WHERE DrinkId = @id", new { id }) ?? 0;
    }

    public List<ViewRow> GetMostViewed(int top = 10)
    {
        using var c = Open();
        return c.Query<ViewRow>(
            "SELECT DrinkName, Count FROM Views ORDER BY Count DESC LIMIT @top",
            new { top }).ToList();
    }

    public bool IsFavorite(string id)
    {
        using var c = Open();
        return c.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Favorites WHERE DrinkId = @id", new { id }) > 0;
    }

    // Adds if missing, removes if present. Returns the NEW state (true = now a favorite).
    public bool ToggleFavorite(string id, string name)
    {
        using var c = Open();
        if (IsFavorite(id))
        {
            c.Execute("DELETE FROM Favorites WHERE DrinkId = @id", new { id });
            return false;
        }
        c.Execute("INSERT INTO Favorites (DrinkId, DrinkName) VALUES (@id, @name)",
            new { id, name });
        return true;
    }

    public List<FavoriteRow> GetFavorites()
    {
        using var c = Open();
        return c.Query<FavoriteRow>(
            "SELECT DrinkId, DrinkName FROM Favorites ORDER BY DrinkName").ToList();
    }
}
```

**Why it's built this way**
- **`Data Source=drinks.db`** — SQLite is a single file; no server, no install. The file is
  created automatically on first connection.
- **`CREATE TABLE IF NOT EXISTS` in the constructor** — schema setup happens once, and it's
  idempotent, so every launch is safe. No migration tooling needed for two tables.
- **`@id`, `@name` parameters** — Dapper binds the anonymous object `new { id, name }` to
  those placeholders. This is parameterized SQL: no string concatenation, no injection.
- **`ON CONFLICT ... DO UPDATE`** — SQLite's upsert. One statement does "insert first time,
  increment every time after" instead of a read-then-write you'd have to guard.
- **`ExecuteScalar<int?>`** — reads a single value; `?? 0` covers "no row yet".
- **`Query<ViewRow>`** — Dapper maps each result row to a `ViewRow` by matching column names
  to record parameters. That's the whole ORM, in one line per query.
- **`using var c = Open()`** — a fresh connection per call, disposed at end of method.
  SQLite connection pooling makes this cheap; it keeps each method self-contained.

---

## Step 5 — `UserInput.cs`: the Spectre UI and the flow

This is the interactive layer: the menus, the recipe screen, the picture, the favorite/view
wiring. Open the blank `UserInput.cs`.

```csharp
using Spectre.Console;

namespace DrinksInfo;

public class UserInput
{
    private static readonly HttpClient ImageHttp = new();  // only for downloading thumbnails
    private readonly IDrinksApi _api;
    private readonly DrinksDatabase _db;

    // Dependencies are passed in (constructor injection), not created here.
    // That's why the same UserInput works with either API implementation.
    public UserInput(IDrinksApi api, DrinksDatabase db)
    {
        _api = api;
        _db = db;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Drinks[/] — what do you want to do?")
                    .AddChoices("Browse drinks", "View favorites", "Most viewed", "Exit"));

            switch (choice)
            {
                case "Browse drinks":  await BrowseAsync(); break;
                case "View favorites": ShowFavorites(); break;
                case "Most viewed":    ShowMostViewed(); break;
                case "Exit": return;       // leaves RunAsync -> app ends
            }
        }
    }

    private async Task BrowseAsync()
    {
        var categories = await _api.GetCategoriesAsync();
        if (categories.Count == 0) { Pause("No categories returned."); return; }

        // SelectionPrompt<Category> uses the ToString() override from Step 1.
        var category = AnsiConsole.Prompt(
            new SelectionPrompt<Category>()
                .Title("Pick a [green]category[/]:")
                .PageSize(15)
                .AddChoices(categories));

        var drinks = await _api.GetDrinksByCategoryAsync(category.Name);
        if (drinks.Count == 0) { Pause("No drinks in that category."); return; }

        var drink = AnsiConsole.Prompt(
            new SelectionPrompt<Drink>()
                .Title($"Pick a [green]drink[/] from {category.Name}:")
                .PageSize(15)
                .AddChoices(drinks));

        await ShowDrinkAsync(drink.Id);
    }

    private async Task ShowDrinkAsync(string id)
    {
        var d = await _api.GetDrinkByIdAsync(id);
        if (d is null) { Pause("Drink not found."); return; }

        _db.IncrementView(d.Id, d.Name);   // viewing it counts as a view

        AnsiConsole.Clear();
        await RenderPictureAsync(d.Thumbnail);

        AnsiConsole.Write(new Rule($"[yellow]{d.Name}[/]").LeftJustified());
        AnsiConsole.MarkupLine($"Category: [blue]{d.Category}[/]");
        AnsiConsole.MarkupLine($"Viewed: [blue]{_db.GetViewCount(d.Id)}[/] time(s)");
        AnsiConsole.MarkupLine($"Favorite: {(_db.IsFavorite(d.Id) ? "[green]yes[/]" : "no")}");

        var table = new Table().AddColumn("Ingredient").AddColumn("Measure");
        foreach (var (ing, measure) in d.Ingredients())   // the helper from Step 1
            table.AddRow(Markup.Escape(ing), Markup.Escape(measure));
        AnsiConsole.Write(table);

        AnsiConsole.Write(new Panel(Markup.Escape(d.Instructions ?? "No instructions."))
            .Header("Instructions").Expand());

        var fav = _db.IsFavorite(d.Id) ? "Remove from favorites" : "Add to favorites";
        var next = AnsiConsole.Prompt(
            new SelectionPrompt<string>().AddChoices(fav, "Back"));
        if (next == fav)
        {
            var nowFav = _db.ToggleFavorite(d.Id, d.Name);
            Pause(nowFav ? "Added to favorites." : "Removed from favorites.");
        }
    }

    // Download the JPEG bytes and let Spectre render them as colored blocks.
    private static async Task RenderPictureAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var bytes = await ImageHttp.GetByteArrayAsync(url);
            AnsiConsole.Write(new CanvasImage(bytes).MaxWidth(32));
        }
        catch
        {
            // Never let a flaky image download crash the recipe screen.
            AnsiConsole.MarkupLine($"[grey](image unavailable: {Markup.Escape(url)})[/]");
        }
    }

    private void ShowFavorites()
    {
        AnsiConsole.Clear();
        var favs = _db.GetFavorites();
        if (favs.Count == 0) { Pause("No favorites yet."); return; }

        var table = new Table().AddColumn("Favorite drinks");
        foreach (var f in favs) table.AddRow(Markup.Escape(f.DrinkName));
        AnsiConsole.Write(table);
        Pause();
    }

    private void ShowMostViewed()
    {
        AnsiConsole.Clear();
        var rows = _db.GetMostViewed();
        if (rows.Count == 0) { Pause("Nothing viewed yet."); return; }

        var table = new Table().AddColumn("Drink").AddColumn("Views");
        foreach (var r in rows) table.AddRow(Markup.Escape(r.DrinkName), r.Count.ToString());
        AnsiConsole.Write(table);
        Pause();
    }

    private static void Pause(string? msg = null)
    {
        if (msg is not null) AnsiConsole.MarkupLine(Markup.Escape(msg));
        AnsiConsole.MarkupLine("[grey]Press any key...[/]");
        Console.ReadKey(true);
    }
}
```

**Why it's built this way**
- **Constructor injection (`api`, `db` passed in)** — `UserInput` doesn't construct its
  dependencies, so the same UI runs on `HttpClientDrinksApi` or `RestSharpDrinksApi`
  unchanged. This is the whole reason the two versions are comparable.
- **`SelectionPrompt<T>`** — gives you an arrow-key menu and *only* returns one of the
  choices. This is also why we don't need a separate validator: the user literally cannot
  pick an invalid option. `[green]...[/]` is Spectre markup for color.
- **`Markup.Escape(...)`** — drink names/instructions can contain `[` or `]`, which Spectre
  would try to read as markup. Escaping prevents both rendering glitches and crashes. Always
  escape untrusted text before it hits the console.
- **`CanvasImage(bytes).MaxWidth(32)`** — turns the downloaded JPEG into terminal blocks,
  capped at 32 cells wide so it fits. Wrapped in try/catch because an image hiccup must not
  take down the recipe screen.
- **`Table` / `Panel` / `Rule`** — Spectre's layout widgets: a bordered grid for ingredients,
  a boxed panel for instructions, a horizontal title rule.
- **`Pause`** — uniform "press a key" so screens don't flash past.

> **ponytail: delete `Validator.cs`.** The C# Academy lists "don't crash on bad input" as a
> requirement, and Spectre satisfies it natively — `SelectionPrompt` constrains every menu
> choice, and `TextPrompt<T>().Validate(...)` would cover any free-text field (we have none).
> A separate validator class would be an empty wrapper around what the framework already
> guarantees. Native feature covers the requirement, so there's nothing to write.

---

## Step 6 — `Program.cs`: wire it together and pick the implementation

Replace the "Hello, World!" line. This is where you choose **which** API version to run —
the comparison switch.

```csharp
using DrinksInfo;
using Spectre.Console;

// The startup choice that selects the implementation to compare.
var impl = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Which API client?")
        .AddChoices("HttpClient", "RestSharp"));

IDrinksApi api = impl == "RestSharp"
    ? new RestSharpDrinksApi()
    : new HttpClientDrinksApi();

var db = new DrinksDatabase();              // creates/opens drinks.db, ensures tables
await new UserInput(api, db).RunAsync();    // hand both deps to the UI and start
```

**Why it's built this way**
- This is the only place that names a concrete API class. Everything downstream sees the
  `IDrinksApi` interface, so switching `HttpClient` ↔ `RestSharp` changes exactly these lines.
- Top-level statements (no `Main` method) keep startup to what matters: choose, build, run.
- `await ... RunAsync()` — the file is implicitly `async` because you `await`; .NET wires up
  the async entry point for you.

---

## Step 7 — Verify the tricky bit (`Ingredients()` self-check)

The 15-field ingredient loop is the only non-obvious logic. Confirm it before trusting the UI.
Temporarily paste this at the top of `Program.cs`, run once, then delete it:

```csharp
using System.Text.Json;
using System.Diagnostics;
using DrinksInfo;

var json = """
{ "idDrink":"1","strDrink":"Test","strInstructions":"Stir.",
  "strIngredient1":"Gin","strMeasure1":"2 oz",
  "strIngredient2":" Tonic ","strMeasure2":" 4 oz ",
  "strIngredient3":null,"strMeasure3":null }
""";
var d = JsonSerializer.Deserialize<DrinkDetail>(json)!;
var list = d.Ingredients().ToList();
Debug.Assert(list.Count == 2, "should skip the null ingredient");
Debug.Assert(list[1] == ("Tonic", "4 oz"), "should trim surrounding whitespace");
Console.WriteLine("Ingredients() OK");
```

Run with `dotnet run`. You want `Ingredients() OK` and no assertion failure. This proves the
null-slot skipping and whitespace trimming work — the two things most likely to be wrong.

---

## Step 8 — Run the whole app end-to-end

```sh
dotnet build      # compiles clean with all five packages
dotnet run
```

Walk this checklist:
1. Pick **HttpClient** → **Browse drinks** → pick a category → pick a drink.
   - Picture renders as blocks, ingredient table + instructions show, "Viewed: 1".
2. Go back into the same drink → "Viewed: 2" (SQLite persisted the count).
3. On a drink, choose **Add to favorites** → main menu → **View favorites** lists it.
4. **Most viewed** shows drinks ordered by count.
5. Quit, relaunch, this time pick **RestSharp** → every screen behaves identically.
   That identical behavior across both clients is the success criterion for the comparison.
6. Confirm favorites and counts survived the restart (they live in `drinks.db` in the
   working directory).

---

## Recap — what you built and why it's shaped this way
- **One interface, two clients** → swap the entire network layer by changing two lines in
  `Program.cs`; the UI never notices. That's the comparison, made honest by the interface.
- **`[JsonExtensionData]` + a loop** → 15 messy fields become a clean `(ingredient, measure)`
  list without 30 properties.
- **Dapper + SQLite upsert** → favorites and view counts persist with one short method each,
  no ORM ceremony, no server.
- **Spectre prompts** → menus that can't return invalid input, which is why there's no
  validator class to write.
- **Picture in a try/catch** → a nice-to-have that can never crash the core flow.

Total new code: `Models.cs`, `DrinksDatabase.cs`, and filling in the three scaffolding files.
`Validator.cs` gets deleted.
