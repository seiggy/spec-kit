using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Specify.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Root command with description
        var root = new RootCommand("Spec-Driven Development Toolkit (Specify)");

    // Global version option (manual implementation)
    var versionOption = new Option<bool>("--version");
    // Beta7 minimal API: description not in this ctor; help text handled via SetAction or custom help
    root.Add(versionOption);
    var versionAlias = new Option<bool>("-V");
    root.Add(versionAlias);

        // Common options/arguments for init
    var projectArg = new Argument<string?>("project-name") { Arity = ArgumentArity.ZeroOrOne, Description = "Name for your new project directory (omit with --here)" };
    var aiOption = new Option<string?>("--ai") { Description = "AI assistant to use: claude | gemini | copilot" };
    var ignoreAgentOption = new Option<bool>("--ignore-agent-tools") { Description = "Skip checks for AI agent tools like Claude Code" };
    var noGitOption = new Option<bool>("--no-git") { Description = "Skip git repository initialization" };
    var hereOption = new Option<bool>("--here") { Description = "Initialize project in the current directory instead of creating a new one" };

        var init = new Command("init", "Initialize a new Specify project from the latest template");
    init.Add(projectArg);
    init.Add(aiOption);
    init.Add(ignoreAgentOption);
    init.Add(noGitOption);
    init.Add(hereOption);

        init.SetAction(async (parseResult, ct) =>
        {
            var project = parseResult.GetValue(projectArg);
            var ai = parseResult.GetValue(aiOption);
            var ignoreAgent = parseResult.GetValue(ignoreAgentOption);
            var noGit = parseResult.GetValue(noGitOption);
            var here = parseResult.GetValue(hereOption);
            await InitCommand.HandleAsync(project, ai, ignoreAgent, noGit, here);
            return 0;
        });

        var check = new Command("check", "Check that all required tools are installed");
    check.SetAction(async (pr, ct) => { await CheckCommand.HandleAsync(); return 0; });

    root.Add(init);
    root.Add(check);

        if (args.Length == 0)
        {
            Banner.Show();
            AnsiConsole.MarkupLine("[grey]Run 'specify --help' for usage information[/]");
        }

        var parseResult = root.Parse(args);

        // Handle version printing early
    if (parseResult.GetValue(versionOption) || parseResult.GetValue(versionAlias))
        {
            var asm = typeof(Program).Assembly;
            var ver = asm.GetName().Version?.ToString() ?? "unknown";
            AnsiConsole.MarkupLine($"[green]specify[/] version [yellow]{ver}[/]");
            return 0;
        }

        return await parseResult.InvokeAsync();
    }
}
