using Serilog;
using Serilog.Core;

namespace Arcana.Core.Logging;

public static class LogConfig
{
    public static LoggingLevelSwitch LevelSwitch { get; } = new(Serilog.Events.LogEventLevel.Warning);

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Arcana", "logs");

    public static void Init()
    {
        Directory.CreateDirectory(LogDirectory);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LevelSwitch)
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(LogDirectory, "arcana-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public static void SetLevel(string level)
    {
        LevelSwitch.MinimumLevel = level.ToLowerInvariant() switch
        {
            "trace" => Serilog.Events.LogEventLevel.Verbose,
            "debug" => Serilog.Events.LogEventLevel.Debug,
            "info" => Serilog.Events.LogEventLevel.Information,
            "warn" or "warning" => Serilog.Events.LogEventLevel.Warning,
            "error" => Serilog.Events.LogEventLevel.Error,
            "fatal" => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Warning,
        };
    }
}
