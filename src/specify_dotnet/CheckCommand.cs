using Spectre.Console;

namespace Specify.Cli;

public static class CheckCommand
{
    public static Task HandleAsync()
    {
        Banner.Show();
        AnsiConsole.MarkupLine("[bold]Checking Specify requirements...[/]\n");
        AnsiConsole.MarkupLine("[cyan]Checking internet connectivity...[/]");
        try
        {
            using var http = new HttpClient();
            using var resp = http.GetAsync("https://api.github.com").Result;
            AnsiConsole.MarkupLine("[green]✓[/] Internet connection available");
        }
        catch
        {
            AnsiConsole.MarkupLine("[red]✗[/] No internet connection - required for downloading templates");
            AnsiConsole.MarkupLine("[yellow]Please check your internet connection[/]");
        }

        AnsiConsole.MarkupLine("\n[cyan]Optional tools:[/]");
        ToolCheck.PrintCheck("git", "https://git-scm.com/downloads");

        AnsiConsole.MarkupLine("\n[cyan]Optional AI tools:[/]");
        ToolCheck.PrintCheck("claude", "Install from: https://docs.anthropic.com/en/docs/claude-code/setup");
        ToolCheck.PrintCheck("gemini", "Install from: https://github.com/google-gemini/gemini-cli");

        AnsiConsole.MarkupLine("\n[green]✓ Specify CLI is ready to use![/]");
        return Task.CompletedTask;
    }
}
