using Spectre.Console;
using Spectre.Console.Rendering;

namespace Specify.Cli;

public sealed class StepTracker
{
    private readonly List<Step> _steps = new();
    private readonly string _title;
    public StepTracker(string title) => _title = title;

    public void Add(string key, string label)
    {
        if (_steps.All(s => s.Key != key))
            _steps.Add(new Step(key, label));
    }
    public void AddRange(IEnumerable<(string key, string label)> steps)
    {
        foreach (var (k,l) in steps) Add(k,l);
    }
    public void Start(string key, string? detail = null) => Update(key, StepStatus.Running, detail);
    public void Complete(string key, string? detail = null) => Update(key, StepStatus.Done, detail);
    public void Error(string key, string? detail = null) => Update(key, StepStatus.Error, detail);
    public void Skip(string key, string? detail = null) => Update(key, StepStatus.Skipped, detail);

    private void Update(string key, StepStatus status, string? detail)
    {
        var step = _steps.FirstOrDefault(s => s.Key == key);
        if (step is null)
        {
            step = new Step(key, key);
            _steps.Add(step);
        }
        step.Status = status;
        if (!string.IsNullOrWhiteSpace(detail)) step.Detail = detail;
    }

    public IRenderable Render()
    {
        var tree = new Tree($"[bold cyan]{_title}[/]");
        foreach (var step in _steps)
        {
            var symbol = step.Status switch
            {
                StepStatus.Done => "[green]●[/]",
                StepStatus.Pending => "[green dim]○[/]",
                StepStatus.Running => "[cyan]○[/]",
                StepStatus.Error => "[red]●[/]",
                StepStatus.Skipped => "[yellow]○[/]",
                _ => ""
            };
            var label = step.Status == StepStatus.Pending
                ? $"{symbol} [grey]{step.Label}{(string.IsNullOrWhiteSpace(step.Detail)?"":$" ({step.Detail})")}[/]"
                : $"{symbol} [white]{step.Label}[/]" + (string.IsNullOrWhiteSpace(step.Detail)?"":$" [grey]({step.Detail})[/]");
            tree.AddNode(label);
        }
        return tree;
    }

    private sealed class Step
    {
        public Step(string key, string label) { Key = key; Label = label; }
        public string Key { get; }
        public string Label { get; }
        public StepStatus Status { get; set; } = StepStatus.Pending;
        public string? Detail { get; set; }
    }
    private enum StepStatus { Pending, Running, Done, Error, Skipped }
}
