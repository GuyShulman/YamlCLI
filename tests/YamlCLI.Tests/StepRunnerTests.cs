using YamlCLI.Actions;
using YamlCLI.Execution;
using YamlCLI.Models;
using YamlCLI.Parsing;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Tests;

/// <summary>
/// Tests for the StepRunner - validates execution order, dry-run mode,
/// verbose mode, and fail-fast error handling.
/// </summary>
[Collection("ConsoleTests")]
public class StepRunnerTests
{
    [Fact]
    public async Task RunAsync_ExecutesStepsInOrder()
    {
        var yaml = """
            steps:
              - action: set-var
                name: step1
                value: "done"
              - action: set-var
                name: step2
                value: "done"
              - action: set-var
                name: step3
                value: "done"
            """;

        var (success, context) = await RunYaml(yaml);

        Assert.True(success);
        Assert.Equal("done", context.GetVariable("step1"));
        Assert.Equal("done", context.GetVariable("step2"));
        Assert.Equal("done", context.GetVariable("step3"));
    }

    [Fact]
    public async Task RunAsync_DryRunDoesNotExecute()
    {
        var yaml = """
            steps:
              - action: set-var
                name: myVar
                value: "should-not-be-set"
            """;

        var (success, context) = await RunYaml(yaml, dryRun: true);

        Assert.True(success);
        Assert.False(context.TryGetVariable("myVar", out _));
    }

    [Fact]
    public async Task RunAsync_FailFastOnAssertionFailure()
    {
        var yaml = """
            steps:
              - action: set-var
                name: before
                value: "set"
              - action: assert
                condition: "1 == 2"
              - action: set-var
                name: after
                value: "set"
            """;

        var (success, context) = await RunYaml(yaml);

        Assert.False(success);
        Assert.Equal("set", context.GetVariable("before"));
        Assert.False(context.TryGetVariable("after", out _)); // Should not reach this step
    }

    [Fact]
    public async Task RunAsync_VerboseMode_IncludesExtraOutput()
    {
        var yaml = """
            steps:
              - action: log
                message: "Hello"
            """;

        var output = await RunYamlCaptureOutput(yaml, verbose: true);

        Assert.Contains("Executing log", output);
        Assert.Contains("Completed in", output);
    }

    [Fact]
    public async Task RunAsync_EmptySteps_ReturnsSuccess()
    {
        var registry = new ActionRegistry();
        var console = new ConsoleWriter();
        var runner = new StepRunner(registry, console);
        var context = new ExecutionContext();

        var result = await CaptureOutput(() => runner.RunAsync(new List<StepDefinition>(), context));

        Assert.True(result);
    }

    [Fact]
    public async Task RunAsync_SummaryShowsCorrectCounts()
    {
        var yaml = """
            steps:
              - action: log
                message: "Step 1"
              - action: log
                message: "Step 2"
            """;

        var output = await RunYamlCaptureOutput(yaml);

        Assert.Contains("2/2 succeeded", output);
        Assert.Contains("0 failed", output);
        Assert.Contains("PASSED", output);
    }

    [Fact]
    public async Task RunAsync_FailureSummaryShowsFailedCount()
    {
        var yaml = """
            steps:
              - action: log
                message: "OK step"
              - action: assert
                condition: "1 == 99"
            """;

        var output = await RunYamlCaptureOutput(yaml);

        Assert.Contains("FAILED", output);
        Assert.Contains("1 failed", output);
    }

    [Fact]
    public async Task RunAsync_FailureSummaryShowsExecutedAndSkippedCount()
    {
        var yaml = """
            steps:
              - action: log
                message: "OK step"
              - action: assert
                condition: "1 == 99"
              - action: log
                message: "Never reached"
            """;

        var output = await RunYamlCaptureOutput(yaml);

        Assert.Contains("FAILED", output);
        Assert.Contains("1/2 succeeded", output);
        Assert.Contains("1 failed", output);
        Assert.Contains("1 skipped", output);
    }

    // --- Helper methods ---

    private static async Task<(bool success, ExecutionContext context)> RunYaml(
        string yaml, bool dryRun = false, bool verbose = false)
    {
        var parser = new YamlParser();
        var steps = parser.ParseYaml(yaml);
        var registry = new ActionRegistry();
        var console = new ConsoleWriter();
        var runner = new StepRunner(registry, console);

        var context = new ExecutionContext
        {
            IsDryRun = dryRun,
            IsVerbose = verbose
        };

        var originalOut = Console.Out;
        Console.SetOut(new StringWriter());
        try
        {
            var success = await runner.RunAsync(steps, context);
            return (success, context);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static async Task<string> RunYamlCaptureOutput(
        string yaml, bool dryRun = false, bool verbose = false)
    {
        var parser = new YamlParser();
        var steps = parser.ParseYaml(yaml);
        var registry = new ActionRegistry();
        var console = new ConsoleWriter();
        var runner = new StepRunner(registry, console);

        var context = new ExecutionContext
        {
            IsDryRun = dryRun,
            IsVerbose = verbose
        };

        var originalOut = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            await runner.RunAsync(steps, context);
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static async Task<T> CaptureOutput<T>(Func<Task<T>> action)
    {
        var originalOut = Console.Out;
        Console.SetOut(new StringWriter());
        try
        {
            return await action();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
