using YamlCLI.Actions;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Execution;

/// <summary>
/// Default implementation of IStepExecutor that resolves actions from an ActionRegistry
/// and executes them with nested dry-run and verbose logging support.
/// </summary>
public class StepExecutor : IStepExecutor
{
    private readonly ActionRegistry _registry;

    /// <summary>
    /// Initializes a new instance of StepExecutor with the specified or default ActionRegistry.
    /// </summary>
    public StepExecutor(ActionRegistry? registry = null)
    {
        _registry = registry ?? new ActionRegistry();
    }

    /// <inheritdoc />
    public async Task ExecuteStepAsync(StepDefinition step, ExecutionContext context)
    {
        var action = _registry.GetAction(step.Action);
        action.Validate(step);

        if (context.IsDryRun)
        {
            context.Console.Message($"  [nested dry-run] {step.Action}");
            return;
        }

        if (context.IsVerbose)
        {
            context.Console.Info($"Executing nested {step.Action}: {step}");
        }

        await action.ExecuteAsync(step, context);
    }

    /// <inheritdoc />
    public async Task ExecuteStepsAsync(List<StepDefinition> steps, ExecutionContext context)
    {
        foreach (var step in steps)
        {
            await ExecuteStepAsync(step, context);
        }
    }
}
