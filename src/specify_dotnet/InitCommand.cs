using Spectre.Console;
using Octokit;
using System.IO.Compression;

namespace Specify.Cli;

public static class InitCommand
{
    private static readonly Dictionary<string, string> AiChoices = new()
    {
        ["copilot"] = "GitHub Copilot",
        ["claude"] = "Claude Code",
        ["gemini"] = "Gemini CLI"
    };

    public static async Task HandleAsync(string? projectName, string? ai, bool ignoreAgentTools, bool noGit, bool here)
    {
        Banner.Show();

        if (here && projectName is not null)
        {
            AnsiConsole.MarkupLine("[red]Cannot specify both project name and --here[/]");
            return;
        }
        if (!here && string.IsNullOrWhiteSpace(projectName))
        {
            AnsiConsole.MarkupLine("[red]Must specify a project name or use --here[/]");
            return;
        }

        var cwd = Directory.GetCurrentDirectory();
        var projectPath = here ? cwd : Path.GetFullPath(projectName!);

        // If --here warn about non-empty directory and confirm (mirrors Python behavior)
        if (here)
        {
            var existingItems = Directory.EnumerateFileSystemEntries(projectPath).Take(10).ToList();
            if (existingItems.Count > 0)
            {
                var count = Directory.EnumerateFileSystemEntries(projectPath).Count();
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Current directory is not empty ({count} items)");
                AnsiConsole.MarkupLine("[yellow]Template files will be merged and may overwrite existing files[/]");
                if (!AnsiConsole.Confirm("Do you want to continue?"))
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                    return;
                }
            }
        }

        if (!here && Directory.Exists(projectPath))
        {
            AnsiConsole.MarkupLine($"[red]Directory '{projectName}' already exists[/]");
            return;
        }

        var selectedAi = ai;
        if (selectedAi is not null && !AiChoices.ContainsKey(selectedAi))
        {
            AnsiConsole.MarkupLine($"[red]Invalid AI assistant '{selectedAi}'. Choose from: {string.Join(", ", AiChoices.Keys)}[/]");
            return;
        }
        if (selectedAi is null)
        {
            selectedAi = PromptForAi();
        }

        var panel = new Panel(new Markup($"[bold cyan]Specify Project Setup[/]\\n{(here ? "Initializing in current directory:" : "Creating new project:")} [green]{Path.GetFileName(projectPath)}[/]\\n[grey]{projectPath}[/]"))
            .RoundedBorder()
            .BorderColor(Color.Teal);
        AnsiConsole.Write(panel);

        if (!here)
        {
            Directory.CreateDirectory(projectPath);
        }

        // Tool checks for selected AI assistant (unless ignored)
        if (!ignoreAgentTools)
        {
            bool missing = false;
            switch (selectedAi)
            {
                case "claude":
                    if (!ToolCheck.Exists("claude"))
                    {
                        AnsiConsole.MarkupLine("[red]Claude CLI is required for Claude projects. Install: https://docs.anthropic.com/en/docs/claude-code/setup[/]");
                        missing = true;
                    }
                    break;
                case "gemini":
                    if (!ToolCheck.Exists("gemini"))
                    {
                        AnsiConsole.MarkupLine("[red]Gemini CLI is required for Gemini projects. Install: https://github.com/google-gemini/gemini-cli[/]");
                        missing = true;
                    }
                    break;
            }
            if (missing)
            {
                AnsiConsole.MarkupLine("[yellow]Tip: use --ignore-agent-tools to skip this check[/]");
                return;
            }
        }

        // Download release asset
        var tracker = new StepTracker("Initialize Specify Project");
        tracker.Add("precheck", "Check required tools");
        tracker.Complete("precheck", "ok");
        tracker.Add("ai-select", "Select AI assistant");
        tracker.Complete("ai-select", selectedAi);
        tracker.AddRange([
            ("fetch", "Fetch latest release"),
            ("download", "Download template"),
            ("zip-list", "Archive contents"),
            ("extract", "Extract template"),
            ("extracted-summary", "Extraction summary"),
            ("flatten", "Flatten nested directory"),
            ("cleanup", "Cleanup"),
            ("git", "Initialize git repository"),
            ("final", "Finalize")
        ]);

    await AnsiConsole.Live(tracker.Render()).StartAsync(async ctx =>
        {
            try
            {
                tracker.Start("fetch");
                var (zipPath, meta) = await DownloadLatestTemplateAsync(selectedAi!, tracker);
                tracker.Complete("fetch", $"release {meta.TagName}");
                tracker.Complete("download", meta.AssetName);

                // Zip listing
                try
                {
                    tracker.Start("zip-list");
                    using var archive = ZipFile.OpenRead(zipPath);
                    tracker.Complete("zip-list", $"{archive.Entries.Count} entries");
                }
                catch (Exception zEx)
                {
                    tracker.Error("zip-list", zEx.Message);
                }

                tracker.Start("extract");
                ExtractTemplate(zipPath, projectPath, here, tracker);
                tracker.Complete("extract");

                try { File.Delete(zipPath); } catch { }
                tracker.Complete("cleanup");

                if (!noGit)
                {
                    tracker.Start("git");
                    if (GitHelper.IsGitRepo(projectPath))
                        tracker.Complete("git", "existing repo");
                    else if (ToolCheck.Exists("git"))
                    {
                        if (GitHelper.InitRepo(projectPath))
                            tracker.Complete("git", "initialized");
                        else
                            tracker.Error("git", "failed");
                    }
                    else
                    {
                        tracker.Skip("git", "git not found");
                    }
                }
                else
                {
                    tracker.Skip("git", "--no-git");
                }

                tracker.Complete("final", "project ready");
            }
            catch (Exception ex)
            {
                tracker.Error("final", ex.Message);
            }
        });

        AnsiConsole.Write(tracker.Render());
        AnsiConsole.MarkupLine("\n[bold green]Project ready.[/]");

        // Next steps panel (mirrors Python CLI guidance)
        var steps = new List<string>();
        int stepNum = 1;
        if (!here)
        {
            steps.Add($"{stepNum}. [bold green]cd {Path.GetFileName(projectPath)}[/]");
        }
        else
        {
            steps.Add($"{stepNum}. You're already in the project directory!");
        }
        stepNum++;
        switch (selectedAi)
        {
            case "claude":
                steps.Add($"{stepNum}. Open in VS Code and start using / commands with Claude Code");
                steps.Add("   - /specify for specifications");
                steps.Add("   - /plan for implementation plans");
                steps.Add("   - /tasks for task generation");
                break;
            case "gemini":
                steps.Add($"{stepNum}. Use Gemini CLI / commands");
                steps.Add("   - gemini /specify for specifications");
                steps.Add("   - gemini /plan for plans");
                steps.Add("   - See GEMINI.md for more");
                break;
            case "copilot":
                steps.Add($"{stepNum}. Open in VS Code and use /specify, /plan, /tasks with GitHub Copilot");
                break;
        }
        stepNum++;
        steps.Add($"{stepNum}. Update [bold magenta]CONSTITUTION.md[/] with your project's principles");

        var stepsPanel = new Panel(string.Join('\n', steps))
            .RoundedBorder()
            .BorderColor(Color.Teal)
            .Header("Next steps", Justify.Center);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(stepsPanel);
    }

    private static string PromptForAi()
    {
        var prompt = new SelectionPrompt<string>()
            .Title("Choose your AI assistant:")
            .AddChoices(AiChoices.Keys);
        return AnsiConsole.Prompt(prompt);
    }

    private static async Task<(string ZipPath, ReleaseMetadata Meta)> DownloadLatestTemplateAsync(string ai, StepTracker tracker)
    {
        var gh = new GitHubClient(new ProductHeaderValue("specify-cli"));
        var release = await gh.Repository.Release.GetLatest("github", "spec-kit");
        var pattern = $"spec-kit-template-{ai}";
        var asset = release.Assets.FirstOrDefault(a => a.Name.Contains(pattern) && a.Name.EndsWith(".zip"));
        if (asset is null)
        {
            throw new Exception($"No template asset found for AI '{ai}'");
        }

        var tempFile = Path.Combine(Path.GetTempPath(), asset.Name);
        tracker.Start("download");

        using var http = new HttpClient();
        using var resp = await http.GetAsync(asset.BrowserDownloadUrl);
        resp.EnsureSuccessStatusCode();
        await using var fs = File.Create(tempFile);
        await resp.Content.CopyToAsync(fs);

        return (tempFile, new ReleaseMetadata(release.TagName, asset.Name));
    }

    private static void ExtractTemplate(string zipPath, string projectPath, bool here, StepTracker tracker)
    {
        tracker.Start("extract");
        var tempRoot = Path.Combine(Path.GetTempPath(), "specify_extract_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        ZipFile.ExtractToDirectory(zipPath, tempRoot);

        var topItems = Directory.GetFileSystemEntries(tempRoot);
        tracker.Start("extracted-summary");
        tracker.Complete("extracted-summary", $"{topItems.Length} top-level items");

        string sourceDir = tempRoot;
        if (topItems.Length == 1 && Directory.Exists(topItems[0]))
        {
            sourceDir = topItems[0];
            tracker.Complete("flatten", "applied");
        }
        else
        {
            // mark flatten as skipped if no need
            tracker.Skip("flatten", "not needed");
        }

        if (here)
        {
            // Merge into current directory
            CopyDirectory(sourceDir, projectPath, overwrite: true);
        }
        else
        {
            CopyDirectory(sourceDir, projectPath, overwrite: true);
        }
        Directory.Delete(tempRoot, true);
    }

    private static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
    {
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(sourceDir, destDir));
        }
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var target = file.Replace(sourceDir, destDir);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: overwrite);
        }
    }

    private record ReleaseMetadata(string TagName, string AssetName);
}
