using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Interface for all step action implementations.
/// Each action type (log, delay, assert, etc.) implements this interface.
/// Actions are auto-discovered by the ActionRegistry via reflection.
/// </summary>
public interface IStepAction
{
    /// <summary>
    /// The action type identifier that maps to the 'action' field in YAML.
    /// Must be lowercase and hyphenated (e.g., "log", "set-var", "print-var").
    /// </summary>
    string ActionType { get; }

    /// <summary>
    /// Executes the action with the given step definition and execution context.
    /// </summary>
    /// <param name="step">The parsed step definition containing action-specific properties.</param>
    /// <param name="context">The shared execution context with variables, flags, and console writer.</param>
    Task ExecuteAsync(StepDefinition step, ExecutionContext context);
}
