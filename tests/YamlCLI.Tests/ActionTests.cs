using YamlCLI.Actions;
using YamlCLI.Execution;
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
    public void Interpolate_VariablesWithHyphensAndDots_AreResolvedCorrectly()
    {
        _context.SetVariable("user-id", "12345");
        _context.SetVariable("api.url", "https://api.test.com");
        _context.SetVariable("nested_name", "alpha");

        var result = _context.Interpolate("Fetching ${api.url}/users/${user-id}?type=${nested_name}");

        Assert.Equal("Fetching https://api.test.com/users/12345?type=alpha", result);
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
    public async Task AssertAction_DoesNotReplaceVariableSubstringInsideStringLiteral()
    {
        var action = new AssertAction();
        _context.SetVariable("x", 10);
        _context.SetVariable("animal", "foxes");
        // "foxes" contains "x", but should not be replaced with "fo10es"
        var step = CreateStep("assert", ("condition", "animal == \"foxes\""));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));
    }

    [Fact]
    public async Task AssertAction_WithManyVariables_EvaluatesCorrectly()
    {
        var action = new AssertAction();
        for (int i = 0; i < 25; i++)
        {
            _context.SetVariable($"var_{i}", i);
        }
        _context.SetVariable("target", 42);
        var step = CreateStep("assert", ("condition", "target == 42 && var_10 > 5"));

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
    public void ActionRegistry_ConstructorWithAssemblies_DiscoversActions()
    {
        var registry = new ActionRegistry(typeof(IStepAction).Assembly);

        Assert.True(registry.HasAction("log"));
        Assert.True(registry.HasAction("assert"));
        Assert.Equal(11, registry.GetRegisteredActions().Count());
    }

    [Fact]
    public void ActionRegistry_RegisterFactory_OverridesActionWithDependencyInjection()
    {
        var customHttpClient = new HttpClient();
        var registry = new ActionRegistry();

        // Register HttpAction with injected custom HttpClient
        registry.RegisterFactory("http", () => new HttpAction(customHttpClient));

        var action = registry.GetAction("http");
        Assert.IsType<HttpAction>(action);
    }

    [Fact]
    public void ActionRegistry_RegisterFactory_CreatesFreshInstancePerInvocation()
    {
        var registry = new ActionRegistry();

        var instance1 = registry.GetAction("log");
        var instance2 = registry.GetAction("log");

        // Discovered factories produce fresh instances
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public async Task ExecutionContext_ExecuteStepsAsync_ExecutesAllSteps()
    {
        var steps = new List<StepDefinition>
        {
            CreateStep("set-var", ("name", "k1"), ("value", "v1")),
            CreateStep("set-var", ("name", "k2"), ("value", "v2"))
        };

        await _context.ExecuteStepsAsync(steps);

        Assert.Equal("v1", _context.GetVariable("k1"));
        Assert.Equal("v2", _context.GetVariable("k2"));
    }

    [Fact]
    public void StepDefinition_GetOptionalInt_ReturnsDefaultWhenMissing()
    {
        var step = new StepDefinition { Action = "test" };
        Assert.Equal(42, step.GetOptionalInt("missing", 42));
    }

    [Fact]
    public void StepDefinition_GetRequiredInt_ThrowsWhenNotAnInt()
    {
        var step = new StepDefinition { Action = "test" };
        step.Properties["count"] = "not-a-number";

        Assert.Throws<StepExecutionException>(() => step.GetRequiredInt("count"));
    }

    [Fact]
    public void ActionRegistry_UnknownAction_ThrowsException()
    {
        var registry = new ActionRegistry();

        Assert.Throws<StepExecutionException>(() => registry.GetAction("unknown-action"));
    }

    [Fact]
    public void AllStepActions_MeetArchitecturalStandards()
    {
        var actionTypes = typeof(IStepAction).Assembly.GetTypes()
            .Where(t => typeof(IStepAction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        Assert.NotEmpty(actionTypes);

        var actionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in actionTypes)
        {
            // 1. Must have a public parameterless constructor
            var ctor = type.GetConstructor(Type.EmptyTypes);
            Assert.True(ctor != null && ctor.IsPublic,
                $"Action '{type.FullName}' must have a public parameterless constructor.");

            // 2. Must instantiate cleanly
            var instance = (IStepAction)Activator.CreateInstance(type)!;
            Assert.NotNull(instance);

            // 3. ActionType must be a non-empty, lowercase string
            Assert.False(string.IsNullOrWhiteSpace(instance.ActionType),
                $"Action '{type.FullName}' has an invalid empty ActionType.");
            Assert.Equal(instance.ActionType.ToLowerInvariant(), instance.ActionType);

            // 4. ActionType must be unique across all classes
            Assert.True(actionNames.Add(instance.ActionType),
                $"Duplicate ActionType '{instance.ActionType}' in '{type.FullName}'.");

            // 5. AllowedProperties must be defined and valid
            Assert.NotNull(instance.AllowedProperties);
            Assert.All(instance.AllowedProperties, prop =>
            {
                Assert.False(string.IsNullOrWhiteSpace(prop));
                Assert.Equal(prop.ToLowerInvariant(), prop);
            });
        }
    }

    [Fact]
    public void ActionRegistry_DetectsMissingParameterlessConstructor_ThrowsInvalidOperationException()
    {
        var registry = new ActionRegistry();

        var ex = Assert.Throws<InvalidOperationException>(
            () => registry.DiscoverFromTypes(new[] { typeof(ActionWithoutDefaultCtor) }));

        Assert.Contains("does not have a public parameterless constructor", ex.Message);
    }

    [Fact]
    public void ActionRegistry_DetectsDuplicateActionName_ThrowsInvalidOperationException()
    {
        var registry = new ActionRegistry();

        var ex = Assert.Throws<InvalidOperationException>(
            () => registry.DiscoverFromTypes(new[] { typeof(DuplicateLogAction) }));

        Assert.Contains("Duplicate action name 'log' detected", ex.Message);
    }

    [Fact]
    public void ActionRegistry_DetectsEmptyActionType_ThrowsInvalidOperationException()
    {
        var registry = new ActionRegistry();

        var ex = Assert.Throws<InvalidOperationException>(
            () => registry.DiscoverFromTypes(new[] { typeof(EmptyNameAction) }));

        Assert.Contains("returned a null, empty, or whitespace ActionType", ex.Message);
    }

    [Fact]
    public void ActionRegistry_RegisterAction_RegistersActionInstance()
    {
        var registry = new ActionRegistry();
        var mockAction = new DuplicateLogAction(); // ActionType => "log"
        registry.RegisterAction(mockAction);

        var retrieved = registry.GetAction("log");
        Assert.Same(mockAction, retrieved);
    }

    [Fact]
    public void ActionRegistry_ConstructorThrowsException_ThrowsInvalidOperationException()
    {
        var registry = new ActionRegistry();

        var ex = Assert.Throws<InvalidOperationException>(
            () => registry.DiscoverFromTypes(new[] { typeof(FailingConstructorAction) }));

        Assert.Contains("Failed to instantiate action class", ex.Message);
    }

    [Fact]
    public void StepDefinition_GetRequiredInt_MissingKey_ThrowsException()
    {
        var step = new StepDefinition { Action = "test" };

        var ex = Assert.Throws<StepExecutionException>(() => step.GetRequiredInt("missingKey"));
        Assert.Contains("Missing required property 'missingKey'", ex.Message);
    }

    [Fact]
    public void StepDefinition_GetOptionalInt_VariousTypesAndDefaults()
    {
        var step = new StepDefinition { Action = "test" };

        // Missing key returns default
        Assert.Equal(10, step.GetOptionalInt("missing", 10));

        // Long value
        step.Properties["longVal"] = 123456789L;
        Assert.Equal(123456789, step.GetOptionalInt("longVal"));

        // Parseable string
        step.Properties["strVal"] = "42";
        Assert.Equal(42, step.GetOptionalInt("strVal"));

        // Unparseable value returns default
        step.Properties["invalidVal"] = "not_a_number";
        Assert.Equal(99, step.GetOptionalInt("invalidVal", 99));
    }

    [Fact]
    public void StepDefinition_GetNestedStepsAndStep_ReturnsExpected()
    {
        var step = new StepDefinition { Action = "test" };

        // Missing returns null
        Assert.Null(step.GetNestedSteps("missing"));
        Assert.Null(step.GetNestedStep("missing"));

        // Present List<StepDefinition>
        var nestedList = new List<StepDefinition> { new StepDefinition { Action = "sub" } };
        step.Properties["steps"] = nestedList;
        Assert.Same(nestedList, step.GetNestedSteps("steps"));

        // Present StepDefinition
        var singleStep = new StepDefinition { Action = "single" };
        step.Properties["step"] = singleStep;
        Assert.Same(singleStep, step.GetNestedStep("step"));
    }

    [Fact]
    public void StepDefinition_GetSteps_WithInvalidType_ReturnsEmptyList()
    {
        var step = new StepDefinition { Action = "test" };
        step.Properties["invalidSteps"] = 12345;

        var result = step.GetSteps("invalidSteps");
        Assert.Empty(result);
    }

    [Fact]
    public void StepDefinition_ToString_FormatsCorrectly()
    {
        var step = new StepDefinition { Action = "myAction" };
        step.Properties["key1"] = "val1";
        step.Properties["key2"] = 2;

        var str = step.ToString();
        Assert.Contains("[myAction]", str);
        Assert.Contains("key1=val1", str);
        Assert.Contains("key2=2", str);
    }

    [Fact]
    public void ExecutionContext_Interpolate_UnresolvedVariable_LeavesAsIs()
    {
        var context = new YamlCLI.Models.ExecutionContext();
        var result = context.Interpolate("Hello ${nonexistent} World");
        Assert.Equal("Hello ${nonexistent} World", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConditionEvaluator_EmptyOrWhitespace_ThrowsException(string condition)
    {
        var context = new YamlCLI.Models.ExecutionContext();
        var ex = Assert.Throws<StepExecutionException>(() => Execution.ConditionEvaluator.Evaluate(condition, context));
        Assert.Contains("Condition expression cannot be empty", ex.Message);
    }

    [Fact]
    public void ConditionEvaluator_InvalidSyntax_ThrowsException()
    {
        var context = new YamlCLI.Models.ExecutionContext();
        var ex = Assert.Throws<StepExecutionException>(() => Execution.ConditionEvaluator.Evaluate("1 == = 2", context));
        Assert.Contains("Failed to evaluate condition", ex.Message);
    }

    [Fact]
    public void ConditionEvaluator_BooleanAndObjectVariables_EvaluatesCorrectly()
    {
        var context = new YamlCLI.Models.ExecutionContext();
        context.SetVariable("isEnabled", true);
        context.SetVariable("isFinished", false);
        context.SetVariable("myObj", new DateTime(2026, 1, 1));

        Assert.True(Execution.ConditionEvaluator.Evaluate("isEnabled == true", context));
        Assert.True(Execution.ConditionEvaluator.Evaluate("isFinished == false", context));
        Assert.True(Execution.ConditionEvaluator.Evaluate("myObj != \"\"", context));
    }

    [Fact]
    public async Task StepExecutor_Verbose_LogsNestedExecution()
    {
        var context = new YamlCLI.Models.ExecutionContext { IsVerbose = true, Console = new ConsoleWriter() };
        var executor = new StepExecutor();
        var step = CreateStep("log", ("message", "test"));

        var output = await CaptureConsoleOutput(() => executor.ExecuteStepAsync(step, context));
        Assert.Contains("Executing nested log:", output);
    }

    [Fact]
    public async Task HttpAction_GetRequest_SavesStatusAndBody()
    {
        var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent("{\"message\":\"success\"}")
        });
        var client = new HttpClient(mockHandler);
        var action = new HttpAction(client);

        var step = CreateStep("http",
            ("method", "GET"),
            ("url", "https://api.example.com/test"),
            ("save-status", "resStatus"),
            ("save-body", "resBody"));

        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));

        Assert.Equal(200, _context.GetVariable("resStatus"));
        Assert.Equal("{\"message\":\"success\"}", _context.GetVariable("resBody"));
    }

    [Fact]
    public async Task HttpAction_PostRequest_SendsBodyAndSavesResponse()
    {
        var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.Created,
            Content = new StringContent("{\"id\":123}")
        });
        var client = new HttpClient(mockHandler);
        var action = new HttpAction(client);

        _context.SetVariable("userId", 42);
        var step = CreateStep("http",
            ("method", "POST"),
            ("url", "https://api.example.com/items"),
            ("body", "{\"userId\":${userId}}"),
            ("save-status", "createdStatus"));

        _context.IsVerbose = true;
        await CaptureConsoleOutput(() => action.ExecuteAsync(step, _context));
        _context.IsVerbose = false;

        Assert.Equal(201, _context.GetVariable("createdStatus"));
    }

    [Fact]
    public async Task HttpAction_UnsupportedMethod_ThrowsException()
    {
        var action = new HttpAction();
        var step = CreateStep("http",
            ("method", "DELETE"),
            ("url", "https://api.example.com/test"));

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(step, _context));
    }

    [Fact]
    public async Task HttpAction_NetworkFailure_ThrowsStepExecutionException()
    {
        var mockHandler = new MockHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = new HttpClient(mockHandler);
        var action = new HttpAction(client);

        var step = CreateStep("http",
            ("method", "GET"),
            ("url", "https://invalid.example.com"));

        await Assert.ThrowsAsync<StepExecutionException>(
            () => action.ExecuteAsync(step, _context));
    }

    // --- Helper classes & methods ---

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;

        public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;
        public MockHttpMessageHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception != null) throw _exception;
            return Task.FromResult(_response ?? new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

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

    private class ActionWithoutDefaultCtor : IStepAction
    {
        public ActionWithoutDefaultCtor(string requiredArg) { }
        public string ActionType => "no-default-ctor";
        public IReadOnlySet<string> AllowedProperties => new HashSet<string>();
        public Task ExecuteAsync(StepDefinition step, YamlCLI.Models.ExecutionContext context) => Task.CompletedTask;
    }

    private class DuplicateLogAction : IStepAction
    {
        public string ActionType => "log";
        public IReadOnlySet<string> AllowedProperties => new HashSet<string>();
        public Task ExecuteAsync(StepDefinition step, YamlCLI.Models.ExecutionContext context) => Task.CompletedTask;
    }

    private class EmptyNameAction : IStepAction
    {
        public string ActionType => "   ";
        public IReadOnlySet<string> AllowedProperties => new HashSet<string>();
        public Task ExecuteAsync(StepDefinition step, YamlCLI.Models.ExecutionContext context) => Task.CompletedTask;
    }

    private class FailingConstructorAction : IStepAction
    {
        public FailingConstructorAction()
        {
            throw new InvalidOperationException("Boom in constructor");
        }
        public string ActionType => "failing-ctor";
        public IReadOnlySet<string> AllowedProperties => new HashSet<string>();
        public Task ExecuteAsync(StepDefinition step, YamlCLI.Models.ExecutionContext context) => Task.CompletedTask;
    }
}
