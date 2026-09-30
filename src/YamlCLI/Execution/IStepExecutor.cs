using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Execution;

/// <summary>
/// Executes step definitions within an execution context.
/// Encapsulates step resolution, nested dry-run, verbose logging, and invocation.
/// </summary>
public interface IStepExecutor
{
    /// <summary>
    /// Executes a single step within the provided execution context.
    /// </summary>
    Task ExecuteStepAsync(StepDefinition step, ExecutionContext context);

    /// <summary>
    /// Executes a list of steps in sequence within the provided execution context.
    /// </summary>
    Task ExecuteStepsAsync(List<StepDefinition> steps, ExecutionContext context);
}
