using Spectre.Console;

namespace DrinksInfo;

public class UserInput
{
    private static readonly HttpClient ImageHttp = new();  // only for downloading thumbnails
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
                    .Title("[green]Drinks[/] — what do you want to do?")
                    .AddChoices("Browse drinks", "View favorites", "Most viewed", "Exit"));

            switch (choice)
            {
                case "Browse drinks":  await BrowseAsync(); break;
                case "View favorites": ShowFavorites(); break;
                case "Most viewed":    ShowMostViewed(); break;
                case "Exit": return;
            }
        }
    }

    private async Task BrowseAsync()
    {
        var categories = await _api.GetCategoriesAsync();
        if (categories.Count == 0) { Pause("No categories returned."); return; }

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

        _db.IncrementView(d.Id, d.Name);

        AnsiConsole.Clear();
        await RenderPictureAsync(d.Thumbnail);

        AnsiConsole.Write(new Rule($"[yellow]{d.Name}[/]").LeftJustified());
        AnsiConsole.MarkupLine($"Category: [blue]{d.Category}[/]");
        AnsiConsole.MarkupLine($"Viewed: [blue]{_db.GetViewCount(d.Id)}[/] time(s)");
        AnsiConsole.MarkupLine($"Favorite: {(_db.isFavorite(d.Id) ? "[green]yes[/]" : "no")}");

        var table = new Table().AddColumn("Ingredient").AddColumn("Measure");
        foreach (var (ing, measure) in d.Ingredients())
            table.AddRow(Markup.Escape(ing), Markup.Escape(measure));
        AnsiConsole.Write(table);

        AnsiConsole.Write(new Panel(Markup.Escape(d.Instructions ?? "No instructions."))
            .Header("Instructions").Expand());

        var fav = _db.isFavorite(d.Id) ? "Remove from favorites" : "Add to favorites";
        var next = AnsiConsole.Prompt(
            new SelectionPrompt<string>().AddChoices(fav, "Back"));
        if (next == fav)
        {
            var nowFav = _db.ToggleFavorite(d.Id, d.Name);
            Pause(nowFav ? "Added to favorites." : "Removed from favorites.");
        }
    }

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
            AnsiConsole.MarkupLine($"[grey](image unavailable: {Markup.Escape(url)})[/]");
        }
    }

    private void ShowFavorites()
    {
        AnsiConsole.Clear();
        var favs = _db.GetFavoriteDrinks();
        if (favs.Count == 0) { Pause("No favorites yet."); return; }

        var table = new Table().AddColumn("Favorite drinks");
        foreach (var f in favs) table.AddRow(Markup.Escape(f.Name));
        AnsiConsole.Write(table);
        Pause();
    }

    private void ShowMostViewed()
    {
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
