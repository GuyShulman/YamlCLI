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
    /// The set of canonical allowed properties for this action.
    /// Any property present in a step definition not in this set will be rejected.
    /// </summary>
    IReadOnlySet<string> AllowedProperties { get; }

    /// <summary>
    /// Executes the action with the given step definition and execution context.
    /// </summary>
    /// <param name="step">The parsed step definition containing action-specific properties.</param>
    /// <param name="context">The shared execution context with variables, flags, and console writer.</param>
    Task ExecuteAsync(StepDefinition step, ExecutionContext context);
}

/// <summary>
/// Extension methods for IStepAction validation and execution.
/// </summary>
public static class StepActionExtensions
{
    /// <summary>
    /// Validates that all properties in the step definition are recognized canonical properties.
    /// Throws StepExecutionException if any unknown property is encountered.
    /// </summary>
    /// <param name="action">The step action.</param>
    /// <param name="step">The step definition to validate.</param>
    public static void Validate(this IStepAction action, StepDefinition step)
    {
        foreach (var key in step.Properties.Keys)
        {
            if (key == "action") continue;

            if (!action.AllowedProperties.Contains(key))
            {
                var allowed = string.Join(", ", action.AllowedProperties.OrderBy(p => p).Select(p => $"'{p}'"));
                throw new StepExecutionException(
                    $"Unknown property '{key}' for action '{action.ActionType}'. Allowed properties: {allowed}.");
            }
        }
    }
}
