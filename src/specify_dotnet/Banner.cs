using Spectre.Console;

namespace Specify.Cli;

public static class Banner
{
    private static readonly string Ascii = @"X███████╗██████╗ ███████╗ ██████╗██╗███████╗██╗   ██╗X██╔════╝██╔══██╗██╔════╝██╔════╝██║██╔════╝╚██╗ ██╔╝X███████╗██████╔╝█████╗  ██║     ██║█████╗   ╚████╔╝ X╚════██║██╔═══╝ ██╔══╝  ██║     ██║██╔══╝    ╚██╔╝  X███████║██║     ███████╗╚██████╗██║██║        ██║   X╚══════╝╚═╝     ╚══════╝ ╚═════╝╚═╝╚═╝        ╚═╝   X";
    private const string Tagline = "Spec-Driven Development Toolkit";

    public static void Show()
    {
        // Clear a little vertical space
        AnsiConsole.WriteLine();

        var lines = Ascii.Trim('X').Split('X');
        var gradient = new[] { Color.DodgerBlue1, Color.Blue, Color.DeepSkyBlue1, Color.SkyBlue1, Color.LightSkyBlue1, Color.White };        

        var table = new Table()
            .Centered()
            .NoBorder();
        table.AddColumn(new TableColumn("Banner"));

        // Build gradient lines
        for (int i = 0; i < lines.Length; i++)
        {
            var color = gradient[i % gradient.Length];
            table.AddRow(new Markup($"[bold {color.ToString().ToLower()}]{Escape(lines[i])}[/]"));
        }

        var taglineRule = new Rule($"[yellow italic]{Tagline}[/]")
        {
            Style = Style.Parse("grey37")
        };

        var panel = new Panel(table)
            .RoundedBorder()
            .BorderColor(Color.DodgerBlue1)
            .Padding(0,0,0,0)
            .Expand();

        AnsiConsole.Write(panel);
        AnsiConsole.Write(taglineRule);
        AnsiConsole.WriteLine();
    }

    private static string Escape(string s) => s.Replace("[", "[[");
}
