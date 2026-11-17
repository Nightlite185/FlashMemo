using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Services;

// Used before DI is available, including when the normal log directory cannot be created.
internal sealed class StartupLogger : ILogger<SafetyNet>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = $"{DateTimeOffset.Now:O} [{logLevel}] Startup: {formatter(state, exception)}"
            + Environment.NewLine + exception + Environment.NewLine;

        Trace.WriteLine(message);

        try
        {
            File.AppendAllText(
                Path.Combine(Path.GetTempPath(), "FlashMemo-startup.log"),
                message);
        }
        catch
        {
            // Logging must not prevent the startup error dialog and shutdown.
            // Trace remains available if even the temporary directory is inaccessible.
        }
    }
}