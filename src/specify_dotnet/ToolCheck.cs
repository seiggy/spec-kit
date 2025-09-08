using Spectre.Console;
using System.Diagnostics;

namespace Specify.Cli;

public static class ToolCheck
{
    public static bool Exists(string tool)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which",
                Arguments = tool,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi)!;
            proc.WaitForExit(3000);
            return proc.ExitCode == 0;
        }
        catch { return false; }
    }

    public static void PrintCheck(string tool, string installHint)
    {
        if (Exists(tool))
            AnsiConsole.MarkupLine($"[green]✓[/] {tool}");
        else
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] {tool} not found");
            AnsiConsole.MarkupLine($"  Install with: [cyan]{installHint}[/]");
        }
    }
}
