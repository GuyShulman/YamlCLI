using YamlCLI.Actions;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Tests;

/// <summary>
/// Unit tests for the core action implementations.
/// Tests each action in isolation with controlled inputs.
/// </summary>
[Collection("ConsoleTests")]
public class ActionTests
{
    private readonly ExecutionContext _context = new();

    [Fact]
    public async Task LogAction_WritesMessageToConsole()
    {
        var action = new LogAction();
        var step = CreateStep("log", ("message", "Test message"));

        var output = await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Contains("Test message", output);
    }

    [Fact]
    public async Task LogAction_InterpolatesVariables()
    {
        var action = new LogAction();
        _context.SetVariable("name", "World");
        var step = CreateStep("log", ("message", "Hello, ${name}!"));

        var output = await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Contains("Hello, World!", output);
    }

    [Fact]
    public async Task LogAction_MissingMessage_ThrowsException()
    {
        var action = new LogAction();
        var step = CreateStep("log");

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(step, _context));
    }

    [Fact]
    public async Task DelayAction_WaitsForSpecifiedDuration()
    {
        var action = new DelayAction();
        var step = CreateStep("delay", ("duration", 100));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 90, $"Expected at least 90ms, got {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task DelayAction_NegativeDuration_ThrowsException()
    {
        var action = new DelayAction();
        var step = CreateStep("delay", ("duration", -100));

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(step, _context));
    }

    [Fact]
    public async Task AssertAction_TrueCondition_Passes()
    {
        var action = new AssertAction();
        var step = CreateStep("assert", ("condition", "1 + 2 == 3"));

        // Should not throw
        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));
    }

    [Fact]
    public async Task AssertAction_FalseCondition_ThrowsException()
    {
        var action = new AssertAction();
        var step = CreateStep("assert", ("condition", "1 + 2 == 4"));

        var originalOut = Console.Out;
        Console.SetOut(new StringWriter());
        try
        {
            await Assert.ThrowsAsync<StepExecutionException>(
                () => action.ExecuteAsync(step, _context));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task AssertAction_WithVariables_EvaluatesCorrectly()
    {
        var action = new AssertAction();
        _context.SetVariable("x", 10);
        var step = CreateStep("assert", ("condition", "x > 5"));

        // Should not throw
        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));
    }

    [Fact]
    public async Task AssertAction_ComparisonOperators_Work()
    {
        var action = new AssertAction();

        // Test greater than
        var step1 = CreateStep("assert", ("condition", "10 > 5"));
        await CaptureConsoleOutput(() => action.ExecuteAsync(step1, _context));

        // Test less than
        var step2 = CreateStep("assert", ("condition", "3 < 7"));
        await CaptureConsoleOutput(() => action.ExecuteAsync(step2, _context));

        // Test equality
        var step3 = CreateStep("assert", ("condition", "5 == 5"));
        await CaptureConsoleOutput(() => action.ExecuteAsync(step3, _context));

        // Test not equal
        var step4 = CreateStep("assert", ("condition", "5 != 6"));
        await CaptureConsoleOutput(() => action.ExecuteAsync(step4, _context));
    }

    [Fact]
    public async Task SetVarAction_SetsVariableInContext()
    {
        var action = new SetVarAction();
        var step = CreateStep("set-var", ("name", "myVar"), ("value", "myValue"));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Equal("myValue", _context.GetVariable("myVar"));
    }

    [Fact]
    public async Task SetVarAction_InterpolatesValue()
    {
        var action = new SetVarAction();
        _context.SetVariable("host", "example.com");
        var step = CreateStep("set-var", ("name", "url"), ("value", "https://${host}/api"));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Equal("https://example.com/api", _context.GetVariable("url"));
    }

    [Fact]
    public async Task PrintVarAction_PrintsVariableValue()
    {
        var action = new PrintVarAction();
        _context.SetVariable("myVar", "Hello!");
        var step = CreateStep("print-var", ("name", "myVar"));

        var output = await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Contains("myVar", output);
        Assert.Contains("Hello!", output);
    }

    [Fact]
    public async Task PrintVarAction_UndefinedVariable_ThrowsException()
    {
        var action = new PrintVarAction();
        var step = CreateStep("print-var", ("name", "undefinedVar"));

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(step, _context));
    }

    [Fact]
    public void ActionRegistry_DiscoversAllCoreActions()
    {
        var registry = new ActionRegistry();

        Assert.True(registry.HasAction("log"));
        Assert.True(registry.HasAction("delay"));
        Assert.True(registry.HasAction("assert"));
        Assert.True(registry.HasAction("http"));
        Assert.True(registry.HasAction("set-var"));
        Assert.True(registry.HasAction("print-var"));
    }

    [Fact]
    public void ActionRegistry_UnknownAction_ThrowsException()
    {
        var registry = new ActionRegistry();

        Assert.Throws<StepExecutionException>(() => registry.GetAction("unknown-action"));
    }

    // --- Helper methods ---

    private static StepDefinition CreateStep(string action, params (string key, object value)[] properties)
    {
        var step = new StepDefinition { Action = action };
        foreach (var (key, value) in properties)
        {
            step.Properties[key] = value;
        }
        return step;
    }

    private static async Task<string> CaptureConsoleOutput(Func<Task> action)
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            await action();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
