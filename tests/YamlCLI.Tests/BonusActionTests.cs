using YamlCLI.Actions;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Tests;

/// <summary>
/// Unit and integration tests for all 5 bonus actions:
/// parallel, retry, shell, condition, and import.
/// </summary>
[Collection("ConsoleTests")]
public class BonusActionTests : IDisposable
{
    private readonly ExecutionContext _context = new();
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                try { File.Delete(file); } catch { /* ignore cleanup errors */ }
            }
        }
    }

    // ==========================================
    // PARALLEL ACTION TESTS
    // ==========================================

    [Fact]
    public async Task ParallelAction_ExecutesStepsConcurrently()
    {
        var action = new ParallelAction();
        var step1 = CreateStep("set-var", ("name", "p1"), ("value", "v1"));
        var step2 = CreateStep("set-var", ("name", "p2"), ("value", "v2"));

        var parentStep = CreateStep("parallel", ("steps", new List<StepDefinition> { step1, step2 }));

        await CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context));

        Assert.Equal("v1", _context.GetVariable("p1"));
        Assert.Equal("v2", _context.GetVariable("p2"));
    }

    [Fact]
    public async Task ParallelAction_EmptySteps_DoesNotThrow()
    {
        var action = new ParallelAction();
        var parentStep = CreateStep("parallel", ("steps", new List<StepDefinition>()));

        var output = await CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context));

        Assert.Contains("no steps", output);
    }

    [Fact]
    public async Task ParallelAction_StepFails_ThrowsException()
    {
        var action = new ParallelAction();
        var passingStep = CreateStep("log", ("message", "OK"));
        var failingStep = CreateStep("assert", ("condition", "1 == 2"));

        var parentStep = CreateStep("parallel", ("steps", new List<StepDefinition> { passingStep, failingStep }));

        await Assert.ThrowsAnyAsync<StepExecutionException>(
            () => CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context)));
    }

    [Fact]
    public async Task ParallelAction_DryRun_DoesNotExecute()
    {
        var action = new ParallelAction();
        var step = CreateStep("set-var", ("name", "dryKey"), ("value", "dryVal"));
        var parentStep = CreateStep("parallel", ("steps", new List<StepDefinition> { step }));

        _context.IsDryRun = true;
        await CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context));
        _context.IsDryRun = false;

        Assert.False(_context.TryGetVariable("dryKey", out _));
    }

    // ==========================================
    // RETRY ACTION TESTS
    // ==========================================

    [Fact]
    public async Task RetryAction_SucceedsOnFirstAttempt()
    {
        var action = new RetryAction();
        var childStep = CreateStep("set-var", ("name", "retried"), ("value", "firstTry"));
        var parentStep = CreateStep("retry", ("attempts", 3), ("step", childStep));

        await CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context));

        Assert.Equal("firstTry", _context.GetVariable("retried"));
    }

    [Fact]
    public async Task RetryAction_ExhaustsAttempts_ThrowsException()
    {
        var action = new RetryAction();
        var failingStep = CreateStep("assert", ("condition", "1 == 0"));
        var parentStep = CreateStep("retry", ("attempts", 2), ("delay", 10), ("step", failingStep));

        var ex = await Assert.ThrowsAsync<StepExecutionException>(
            () => CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context)));

        Assert.Contains("failed after 2 attempts", ex.Message);
    }

    [Fact]
    public async Task RetryAction_ZeroAttempts_ThrowsException()
    {
        var action = new RetryAction();
        var step = CreateStep("log", ("message", "test"));
        var parentStep = CreateStep("retry", ("attempts", 0), ("step", step));

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(parentStep, _context));
    }

    [Fact]
    public async Task RetryAction_MissingStep_ThrowsException()
    {
        var action = new RetryAction();
        var parentStep = CreateStep("retry", ("attempts", 3));

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(parentStep, _context));
    }

    // ==========================================
    // SHELL ACTION TESTS
    // ==========================================

    [Fact]
    public async Task ShellAction_EchoCommand_LogsOutput()
    {
        var action = new ShellAction();
        var step = CreateStep("shell", ("command", "echo HelloFromShell"));

        var output = await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Contains("HelloFromShell", output);
    }

    [Fact]
    public async Task ShellAction_CaptureVar_SavesOutputInVariable()
    {
        var action = new ShellAction();
        var step = CreateStep("shell",
            ("command", "echo CapturedResult"),
            ("capture-var", "cmdOutput"));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.True(_context.TryGetVariable("cmdOutput", out var value));
        Assert.Contains("CapturedResult", value?.ToString() ?? "");
    }

    [Fact]
    public async Task ShellAction_NonZeroExitCode_ThrowsException()
    {
        var action = new ShellAction();
        // cmd.exe /c exit 42 on Windows, sh -c exit 42 on Linux
        var step = CreateStep("shell", ("command", "exit 42"));

        var ex = await Assert.ThrowsAsync<StepExecutionException>(
            () => CaptureConsoleOutput(() => action.ExecuteAsync(step, _context)));

        Assert.Contains("exited with code 42", ex.Message);
    }

    [Fact]
    public async Task ShellAction_IgnoreError_DoesNotThrowOnNonZero()
    {
        var action = new ShellAction();
        var step = CreateStep("shell",
            ("command", "exit 5"),
            ("ignore-error", true),
            ("capture-exit-code", "exitCode"));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Equal(5, _context.GetVariable("exitCode"));
    }

    [Fact]
    public async Task ShellAction_InterpolatesVariablesInCommand()
    {
        var action = new ShellAction();
        _context.SetVariable("testWord", "InterpolatedEcho");
        var step = CreateStep("shell",
            ("command", "echo ${testWord}"),
            ("capture-var", "interpOutput"));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Contains("InterpolatedEcho", _context.GetVariable("interpOutput")?.ToString() ?? "");
    }

    // ==========================================
    // CONDITION ACTION TESTS
    // ==========================================

    [Fact]
    public async Task ConditionAction_TrueCondition_ExecutesThenBranch()
    {
        var action = new ConditionAction();
        var thenStep = CreateStep("set-var", ("name", "branch"), ("value", "thenRan"));
        var elseStep = CreateStep("set-var", ("name", "branch"), ("value", "elseRan"));

        var parentStep = CreateStep("condition",
            ("if", "10 > 5"),
            ("then", new List<StepDefinition> { thenStep }),
            ("else", new List<StepDefinition> { elseStep }));

        await CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context));

        Assert.Equal("thenRan", _context.GetVariable("branch"));
    }

    [Fact]
    public async Task ConditionAction_FalseCondition_ExecutesElseBranch()
    {
        var action = new ConditionAction();
        var thenStep = CreateStep("set-var", ("name", "branch"), ("value", "thenRan"));
        var elseStep = CreateStep("set-var", ("name", "branch"), ("value", "elseRan"));

        var parentStep = CreateStep("condition",
            ("if", "5 > 10"),
            ("then", new List<StepDefinition> { thenStep }),
            ("else", new List<StepDefinition> { elseStep }));

        await CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context));

        Assert.Equal("elseRan", _context.GetVariable("branch"));
    }

    [Fact]
    public async Task ConditionAction_FalseCondition_NoElse_DoesNothing()
    {
        var action = new ConditionAction();
        var thenStep = CreateStep("set-var", ("name", "branch"), ("value", "thenRan"));

        var parentStep = CreateStep("condition",
            ("if", "1 == 2"),
            ("then", new List<StepDefinition> { thenStep }));

        await CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context));

        Assert.False(_context.TryGetVariable("branch", out _));
    }

    [Fact]
    public async Task ConditionAction_WithVariables_EvaluatesCorrectly()
    {
        var action = new ConditionAction();
        _context.SetVariable("status", 200);
        var thenStep = CreateStep("set-var", ("name", "result"), ("value", "ok"));

        var parentStep = CreateStep("condition",
            ("if", "${status} == 200"),
            ("then", new List<StepDefinition> { thenStep }));

        await CaptureConsoleOutput(() => action.ExecuteAsync(parentStep, _context));

        Assert.Equal("ok", _context.GetVariable("result"));
    }

    [Fact]
    public async Task ConditionAction_MissingIf_ThrowsException()
    {
        var action = new ConditionAction();
        var parentStep = CreateStep("condition",
            ("then", new List<StepDefinition> { CreateStep("log", ("message", "test")) }));

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(parentStep, _context));
    }

    // ==========================================
    // IMPORT ACTION TESTS
    // ==========================================

    [Fact]
    public async Task ImportAction_ValidYamlFile_ExecutesImportedSteps()
    {
        var importedYaml = """
            steps:
              - action: set-var
                name: importedVar
                value: "loadedSuccessfully"
            """;
        var tempFile = Path.Combine(Path.GetTempPath(), $"import_test_{Guid.NewGuid():N}.yaml");
        File.WriteAllText(tempFile, importedYaml);
        _tempFiles.Add(tempFile);

        var action = new ImportAction();
        var step = CreateStep("import", ("file", tempFile));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Equal("loadedSuccessfully", _context.GetVariable("importedVar"));
    }

    [Fact]
    public async Task ImportAction_RelativePath_ResolvesAgainstBasePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"import_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var subFile = Path.Combine(tempDir, "sub.yaml");
        File.WriteAllText(subFile, """
            steps:
              - action: set-var
                name: relativeVar
                value: "fromRelative"
            """);
        _tempFiles.Add(subFile);

        _context.BasePath = tempDir;

        var action = new ImportAction();
        var step = CreateStep("import", ("file", "sub.yaml"));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Equal("fromRelative", _context.GetVariable("relativeVar"));
    }

    [Fact]
    public async Task ImportAction_NonExistentFile_ThrowsException()
    {
        var action = new ImportAction();
        var step = CreateStep("import", ("file", "non-existent-file-12345.yaml"));

        var ex = await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(step, _context));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task ImportAction_MissingFileProperty_ThrowsException()
    {
        var action = new ImportAction();
        var step = CreateStep("import");

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(step, _context));
    }

    // ==========================================
    // REGISTRY DISCOVERY TEST
    // ==========================================

    [Fact]
    public void ActionRegistry_DiscoversAll11Actions()
    {
        var registry = new ActionRegistry();
        var actions = registry.GetRegisteredActions().ToList();

        // 6 core actions
        Assert.Contains("log", actions);
        Assert.Contains("delay", actions);
        Assert.Contains("assert", actions);
        Assert.Contains("http", actions);
        Assert.Contains("set-var", actions);
        Assert.Contains("print-var", actions);

        // 5 bonus actions
        Assert.Contains("parallel", actions);
        Assert.Contains("retry", actions);
        Assert.Contains("shell", actions);
        Assert.Contains("condition", actions);
        Assert.Contains("import", actions);

        Assert.Equal(11, actions.Count);
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
        var sw = new StringWriter();
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
