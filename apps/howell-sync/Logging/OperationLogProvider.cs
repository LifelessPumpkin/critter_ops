using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace HowellSync.Logging;

public sealed class OperationLogOptions
{
    public string Directory { get; set; } = "logs/howell-sync";
    public int RetentionDays { get; set; } = 30;
    public int MaxFileBytes { get; set; } = 10 * 1024 * 1024;
}

// One provider renders the same single-line record to the terminal and an auto-flushed file.
// Each process has its own files, including processes rejected by the catalog lock.
public sealed class OperationLogProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly object gate = new();
    private readonly OperationLogOptions options;
    private readonly TextWriter console;
    private readonly string stem;
    private StreamWriter? file;
    private long bytes;
    private int segment;
    private bool disposed;
    private IExternalScopeProvider scopes = new LoggerExternalScopeProvider();
    public string FilePath { get; private set; } = "";

    public OperationLogProvider(OperationLogOptions options, TextWriter? console = null)
    {
        if (string.IsNullOrWhiteSpace(options.Directory) || options.RetentionDays is < 1 or > 3650 || options.MaxFileBytes is < 1024 or > 100 * 1024 * 1024)
            throw new ArgumentException("Invalid HowellLog directory, retention or file size.");
        this.options = options;
        this.console = console ?? Console.Out;
        var directory = Path.GetFullPath(options.Directory);
        System.IO.Directory.CreateDirectory(directory);
        stem = Path.Combine(directory, $"howell-{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Environment.ProcessId}-{Guid.NewGuid():N}");
        OpenFile(); // Fail startup before catalog work if durable logging cannot be initialized.
        foreach (var old in System.IO.Directory.EnumerateFiles(directory, "howell-*.log"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(old) >= DateTime.UtcNow.AddDays(-options.RetentionDays)) continue;
                // Do not remove a file that another worker still has open.
                using (new FileStream(old, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                File.Delete(old);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Write(LogLevel.Warning, "LOGGING", "Could not remove an expired log file; retention will be retried next invocation.");
            }
        }
    }

    private void OpenFile()
    {
        FilePath = $"{stem}-{segment++:D3}.log";
        var settings = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows()) settings.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        file = new StreamWriter(new FileStream(FilePath, settings), new UTF8Encoding(false)) { AutoFlush = true };
        bytes = 0;
    }

    public ILogger CreateLogger(string categoryName) => new OperationLogger(this);
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => scopes = scopeProvider;

    private static string OneLine(string value) => string.Concat(value.Select(c => char.IsControl(c) ? ' ' : c));

    private void Write(LogLevel level, string label, string message)
    {
        lock (gate)
        {
            if (disposed) return;
            string? run = null;
            scopes.ForEachScope((scope, _) => {
                if (scope is IEnumerable<KeyValuePair<string, object?>> fields)
                    foreach (var field in fields)
                        if (field.Key == "ExecutionId") run = field.Value?.ToString();
            }, 0);
            var severity = level switch { LogLevel.Warning => "WARN", LogLevel.Error or LogLevel.Critical => "ERROR", LogLevel.Debug or LogLevel.Trace => "DEBUG", _ => "INFO" };
            var line = $"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture)} {severity,-5} {OneLine(label),-10} {(run is null ? "" : "run=" + OneLine(run) + " ")}{OneLine(message)}";
            // Bound individual records too; never let a single message bypass rotation limits.
            if (line.Length > options.MaxFileBytes / 4 - 2) line = line[..(options.MaxFileBytes / 4 - 16)] + " [truncated]";
            console.WriteLine(line);
            if (file is null) return;
            try
            {
                var size = Encoding.UTF8.GetByteCount(line + Environment.NewLine);
                if (bytes + size > options.MaxFileBytes)
                {
                    file.Dispose(); file = null;
                    OpenFile();
                }
                file!.WriteLine(line);
                bytes += size;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                try { file?.Dispose(); } catch (IOException) { }
                file = null;
                console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z ERROR LOGGING    File logging failed; remaining messages are console-only. Check log storage.");
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            file?.Dispose();
            file = null;
        }
    }

    private sealed class OperationLogger(OperationLogProvider owner) : ILogger
    {
        public bool IsEnabled(LogLevel level) => level != LogLevel.None;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => owner.scopes.Push(state);
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(level)) owner.Write(level, eventId.Name ?? "WORKER", formatter(state, null));
            // Never append raw exception messages/stacks: those may contain credentials or provider bodies.
        }
    }
}
