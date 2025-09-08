using Spectre.Console;
using System.Diagnostics;

namespace Specify.Cli;

public static class GitHelper
{
    public static bool IsGitRepo(string path)
    {
        try
        {
            var psi = new ProcessStartInfo("git", "rev-parse --is-inside-work-tree")
            {
                WorkingDirectory = path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi)!;
            proc.WaitForExit(4000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool InitRepo(string path)
    {
        try
        {
            Run("git", "init", path);
            Run("git", "add .", path);
            Run("git", "commit -m \"Initial commit from Specify template\"", path);
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error initializing git repo:[/] {ex.Message}");
            return false;
        }
    }

    private static void Run(string file, string args, string cwd)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            throw new Exception(proc.StandardError.ReadToEnd());
        }
    }
}
