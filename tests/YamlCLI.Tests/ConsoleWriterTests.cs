using YamlCLI.Execution;

namespace YamlCLI.Tests;

public class ConsoleWriterTests
{
    [Fact]
    public void CustomTextWriter_RedirectsAllOutput()
    {
        using var sw = new StringWriter();
        var writer = new ConsoleWriter(sw);

        writer.Success("Test success");
        writer.Error("Test error");
        writer.Warning("Test warning");
        writer.Info("Test info");
        writer.StepHeader(1, "test-action", isDryRun: true);
        writer.Message("Custom message");
        writer.Header("Section Header");
        writer.Summary(total: 2, succeeded: 1, failed: 1, TimeSpan.FromMilliseconds(50));

        var output = sw.ToString();

        Assert.Contains("Test success", output);
        Assert.Contains("Test error", output);
        Assert.Contains("Test warning", output);
        Assert.Contains("Test info", output);
        Assert.Contains("[DRY-RUN] Step 1: test-action", output);
        Assert.Contains("Custom message", output);
        Assert.Contains("Section Header", output);
        Assert.Contains("FAILED", output);
        Assert.Contains("Steps: 1/2 succeeded, 1 failed", output);
    }

    [Fact]
    public void CustomTextWriter_SummaryWithSkippedSteps()
    {
        using var sw = new StringWriter();
        var writer = new ConsoleWriter(sw);

        writer.Summary(total: 3, succeeded: 1, failed: 0, TimeSpan.FromMilliseconds(20));

        var output = sw.ToString();
        Assert.Contains("PASSED", output);
        Assert.Contains("(2 skipped)", output);
    }

    [Fact]
    public void CustomTextWriter_NullFallback_DefaultsToConsole()
    {
        // Null text writer falls back to Console.Out without throwing
        var writer = new ConsoleWriter(null);
        Assert.NotNull(writer);
    }
}
