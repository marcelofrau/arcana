using Spectre.Console;
using Spectre.Console.Rendering;

namespace Arcana.Cli;

public static class Output
{
    private static IAnsiConsole? _console;
    private static bool _noColor;

    public static IAnsiConsole Console => _console ??= CreateConsole();

    public static void SetNoColor(bool value)
    {
        _noColor = value;
        _console = null;
    }

    private static IAnsiConsole CreateConsole()
    {
        return AnsiConsole.Create(new AnsiConsoleSettings
        {
            ColorSystem = _noColor ? ColorSystemSupport.NoColors : ColorSystemSupport.Detect,
            Ansi = AnsiSupport.Detect,
        });
    }

    private static Progress Progress => Console.Progress();
    private static Status Status => Console.Status().Spinner(Spinner.Known.Dots);

    public static void MarkupLine(string markup) => Console.MarkupLine(markup);
    public static void Markup(string markup) => Console.Markup(markup);

    public static void Info(string message) => Console.MarkupLine($"[blue]ℹ[/] {message}");
    public static void Success(string message) => Console.MarkupLine($"[green]✔[/] {message}");
    public static void Warning(string message) => Console.MarkupLine($"[yellow]⚠[/] {message}");
    public static void Error(string message) => Console.MarkupLine($"[red]✘[/] {message}");

    public static void Write(IRenderable renderable) => Console.Write(renderable);

    public static void StartProgress(string title, Action<ProgressContext> action)
        => Progress.Start(ctx =>
        {
            ctx.Refresh();
            action(ctx);
        });

    public static async Task StartProgressAsync(string title, Func<ProgressContext, Task> action)
        => await Progress.StartAsync(async ctx =>
        {
            ctx.Refresh();
            await action(ctx);
        });

    public static void StartStatus(string message, Action<StatusContext> action)
        => Status.Start(message, action);

    public static async Task StartStatusAsync(string message, Func<StatusContext, Task> action)
        => await Status.StartAsync(message, action);

    public static async Task<T> StartStatusAsync<T>(string message, Func<StatusContext, Task<T>> action)
        => await Status.StartAsync(message, action);
}
