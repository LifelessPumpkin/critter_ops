using HowellSync.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MaryAnn.Howell;

public sealed class HowellLoggingTests
{
    [Fact]
    public void ConsoleAndFileMatchWithRunContextAndNoMultilineInjection()
    {
        var directory = Path.Combine(Path.GetTempPath(), "howell-log-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var output = new StringWriter();
            using var provider = new OperationLogProvider(new() { Directory = directory }, output);
            var logger = provider.CreateLogger("test");
            using (logger.BeginScope(new Dictionary<string, object?> { ["ExecutionId"] = "123", ["Password"] = "secret" }))
                logger.LogInformation(new EventId(1, "START"), "Import {Name}", "test\nforged\u001b[31m");
            var content = File.ReadAllText(provider.FilePath);
            Assert.Equal(output.ToString(), content);
            Assert.Contains("INFO  START", content); Assert.Contains("run=123", content);
            Assert.DoesNotContain("secret", content); Assert.DoesNotContain('\u001b', content);
            Assert.Single(File.ReadAllLines(provider.FilePath));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void RotatesRetainsRecentFilesAndSeparatesWorkers()
    {
        var directory = Path.Combine(Path.GetTempPath(), "howell-log-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var old = Path.Combine(directory, "howell-old.log"); File.WriteAllText(old, "old"); File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-40));
            var recent = Path.Combine(directory, "howell-recent.log"); File.WriteAllText(recent, "recent");
            var unrelated = Path.Combine(directory, "unrelated.log"); File.WriteAllText(unrelated, "keep"); File.SetLastWriteTimeUtc(unrelated, DateTime.UtcNow.AddDays(-40));
            using var output = new StringWriter();
            using var first = new OperationLogProvider(new() { Directory = directory, MaxFileBytes = 1024 }, output);
            using var second = new OperationLogProvider(new() { Directory = directory }, output);
            Assert.NotEqual(first.FilePath, second.FilePath);
            var logger = first.CreateLogger("test");
            for (var i = 0; i < 30; i++) logger.LogInformation("Record {Index}: {Message}", i, new string('x', 80));
            Assert.False(File.Exists(old)); Assert.True(File.Exists(recent)); Assert.True(File.Exists(unrelated));
            Assert.True(Directory.GetFiles(directory, "howell-*.log").Length > 3);
            Assert.All(Directory.GetFiles(directory), path => Assert.True(new FileInfo(path).Length <= 1024));
            Assert.Contains("Record 29", File.ReadAllText(first.FilePath));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void FiltersHttpChatterAndDoesNotAppendExceptionPayloads()
    {
        var directory = Path.Combine(Path.GetTempPath(), "howell-log-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var output = new StringWriter();
            using var provider = new OperationLogProvider(new() { Directory = directory }, output);
            using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider).SetMinimumLevel(LogLevel.Information));
            var logger = factory.CreateLogger("test");
            logger.LogDebug("Hidden HTTP detail");
            logger.LogError(new Exception("Password=secret"), "Sanitized failure");
            Assert.DoesNotContain("Hidden", output.ToString()); Assert.DoesNotContain("secret", output.ToString());
            Assert.Contains("Sanitized failure", File.ReadAllText(provider.FilePath));
        }
        finally { Directory.Delete(directory, true); }
    }
}
