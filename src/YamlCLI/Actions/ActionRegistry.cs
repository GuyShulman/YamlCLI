using System.Reflection;

namespace YamlCLI.Actions;

/// <summary>
/// Registry that auto-discovers and manages all IStepAction implementations.
/// Uses reflection to scan the assembly, so adding a new action is simply creating a class
/// that implements IStepAction — no registration code needed.
/// </summary>
public class ActionRegistry
{
    private readonly Dictionary<string, IStepAction> _actions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new ActionRegistry and auto-discovers all IStepAction implementations
    /// in the executing assembly.
    /// </summary>
    public ActionRegistry()
    {
        DiscoverActions();
    }

    /// <summary>
    /// Gets the action handler for the given action type.
    /// </summary>
    /// <param name="actionType">The action type string (e.g., "log", "delay").</param>
    /// <returns>The IStepAction implementation for the action type.</returns>
    /// <exception cref="Models.StepExecutionException">Thrown if the action type is unknown.</exception>
    public IStepAction GetAction(string actionType)
    {
        if (_actions.TryGetValue(actionType, out var action))
            return action;

        var available = string.Join(", ", _actions.Keys.OrderBy(k => k));
        throw new Models.StepExecutionException(
            $"Unknown action type '{actionType}'. Available actions: {available}");
    }

    /// <summary>
    /// Checks if an action type is registered.
    /// </summary>
    public bool HasAction(string actionType)
    {
        return _actions.ContainsKey(actionType);
    }

    /// <summary>
    /// Gets all registered action types.
    /// </summary>
    public IEnumerable<string> GetRegisteredActions()
    {
        return _actions.Keys.OrderBy(k => k);
    }

    /// <summary>
    /// Manually registers an action (useful for testing or plugin systems).
    /// </summary>
    public void RegisterAction(IStepAction action)
    {
        _actions[action.ActionType] = action;
    }

    /// <summary>
    /// Scans the executing assembly for all types implementing IStepAction
    /// and registers them.
    /// </summary>
    private void DiscoverActions()
    {
        var actionInterface = typeof(IStepAction);
        var actionTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => actionInterface.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        foreach (var type in actionTypes)
        {
            if (Activator.CreateInstance(type) is IStepAction action)
            {
                _actions[action.ActionType] = action;
            }
        }
    }
}
