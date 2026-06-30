using DrinksInfo;
using Spectre.Console;

var impl = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Which API client?")
        .AddChoices("HttpClient", "RestSharp"));

IDrinksApi api = impl == "RestSharp"
    ? new RestSharpDrinksApi()
    : new HttpClientDrinksApi();

var db = new Database();              // creates/opens drinks.db, ensures tables

await new UserInput(api, db).RunAsync();