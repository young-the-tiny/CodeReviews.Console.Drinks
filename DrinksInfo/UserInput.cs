using Spectre.Console;

namespace DrinksInfo;

public class UserInput
{
    private static readonly HttpClient ImageClient = new() { Timeout = TimeSpan.FromSeconds(3) }; // Downloading thumbnail
    private readonly IDrinksApi _api;
    private readonly Database _db;
    public UserInput(IDrinksApi api, Database db)
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
                .Title("[green]Drinks[/] What do you want to do?")
                .AddChoices("Browse Drinks", "View Favorites", "Most viewed", "Exit"));
            switch (choice)
            {
                case "Browse Drinks": await BrowseAsync(); break;
                case "View Favorites": ShowFavorite(); break;
                case "Most viewed": MostViewed(); break;
                case "Exit": return;
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
        if (d == null) { Pause("No drink retured"); return; }
        _db.IncrementView(id, d.Name);
        AnsiConsole.Clear();
        await RenderPictureAsync(d.Thumbnail);

        AnsiConsole.Write(new Rule($"[yellow]{d.Name}[/]").LeftJustified());
        AnsiConsole.MarkupLine($"Category: [blue]{d.Category}[/]");
        AnsiConsole.MarkupLine($"Name: [blue]{d.Name}[/]");
        AnsiConsole.MarkupLine($"Viewed: [blue]{_db.GetViewCount(d.Id)}[/] time(s)");
        AnsiConsole.MarkupLine($"Favorite: {(_db.isFavorite(d.Id) ? "[green]yes[/]" : "no")}");

        var table = new Table().AddColumn("Ingredient").AddColumn("Measure");
        foreach (var (ing, measure) in d.Ingredients())
        {
            table.AddRow(Markup.Escape(ing), Markup.Escape(measure));
        }
        AnsiConsole.Write(table);
        AnsiConsole.Write(new Panel(Markup.Escape(d.Instructions ?? "No instructions."))
            .Header("Instruction").Expand());

        var fav = _db.isFavorite(d.Id) ? "Remove from favorite" : "Add to favorite";
        var next = AnsiConsole.Prompt(
            new SelectionPrompt<string>().AddChoices(fav, "Back"));
        if (next == fav)
        {
            var nowFavorite = _db.ToggleFavorite(d.Id, d.Name);
            Pause(nowFavorite ? "Added to favorites." : "Removed from favorites.");
        }
    }
    private static async Task RenderPictureAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var bytes = await ImageClient.GetByteArrayAsync(url);
            AnsiConsole.Write(new CanvasImage(bytes).MaxWidth(32));
        }
        catch
        {
            AnsiConsole.MarkupLine($"[grey](image unavailable: {Markup.Escape(url)})[/]");
        }
    }

    private void ShowFavorite()
    {
        AnsiConsole.Clear();
        var favDrink = _db.GetFavoriteDrinks();
        if (favDrink.Count() == 0) { Pause("No favorite drink yet."); return; }
        var table = new Table().AddColumn("Favorite");
        foreach (var f in favDrink) { table.AddRow(Markup.Escape(f.Name)); }
        AnsiConsole.Write(table);
        Pause();
    }
    private void MostViewed()
    {
        // Same logic as showFavorite
        AnsiConsole.Clear();
        var rows = _db.GetMostView();
        if (rows.Count == 0) { Pause("Nothing viewed yet."); return; }

        var table = new Table().AddColumn("Drink").AddColumn("Views");
        foreach (var r in rows) table.AddRow(Markup.Escape(r.Name), r.count.ToString());
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