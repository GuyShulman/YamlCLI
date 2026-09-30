using System.Reflection;

namespace YamlCLI.Actions;

/// <summary>
/// Registry that auto-discovers and manages all IStepAction implementations.
/// Uses reflection to scan assemblies and supports factory-based dependency injection.
/// </summary>
public class ActionRegistry
{
    private readonly Dictionary<string, Func<IStepAction>> _actions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new ActionRegistry and auto-discovers all IStepAction implementations
    /// in the YamlCLI assembly.
    /// </summary>
    public ActionRegistry()
        : this(typeof(IStepAction).Assembly)
    {
    }

    /// <summary>
    /// Creates a new ActionRegistry and auto-discovers all IStepAction implementations
    /// from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for IStepAction implementations.</param>
    public ActionRegistry(params Assembly[] assemblies)
    {
        var targets = assemblies is { Length: > 0 }
            ? assemblies
            : new[] { typeof(IStepAction).Assembly };

        foreach (var asm in targets)
        {
            DiscoverActionsFrom(asm);
        }
    }

    /// <summary>
    /// Gets the action handler for the given action type.
    /// Resolves the action via its registered factory.
    /// </summary>
    /// <param name="actionType">The action type string (e.g., "log", "delay").</param>
    /// <returns>A new or configured IStepAction implementation for the action type.</returns>
    /// <exception cref="Models.StepExecutionException">Thrown if the action type is unknown.</exception>
    public IStepAction GetAction(string actionType)
    {
        if (_actions.TryGetValue(actionType, out var factory))
            return factory();

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
    /// Manually registers an action instance (useful for testing or simple overrides).
    /// </summary>
    public void RegisterAction(IStepAction action)
    {
        RegisterFactory(action.ActionType, () => action);
    }

    /// <summary>
    /// Registers a factory function for creating an action (enables dependency injection).
    /// </summary>
    /// <param name="actionType">The action type string.</param>
    /// <param name="factory">The factory function returning an IStepAction instance.</param>
    public void RegisterFactory(string actionType, Func<IStepAction> factory)
    {
        _actions[actionType] = factory;
    }

    /// <summary>
    /// Scans the given assembly for all types implementing IStepAction
    /// and registers factory functions for them.
    /// </summary>
    private void DiscoverActionsFrom(Assembly assembly)
    {
        var actionInterface = typeof(IStepAction);
        var actionTypes = assembly
            .GetTypes()
            .Where(t => actionInterface.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        DiscoverFromTypes(actionTypes);
    }

    internal void DiscoverFromTypes(IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null || !ctor.IsPublic)
            {
                throw new InvalidOperationException(
                    $"Action class '{type.FullName}' implements IStepAction but does not have a public parameterless constructor. " +
                    $"Either add a public parameterless constructor or register it manually via ActionRegistry.RegisterFactory().");
            }

            IStepAction sample;
            try
            {
                sample = (IStepAction)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to instantiate action class '{type.FullName}' during discovery: {ex.InnerException?.Message ?? ex.Message}", ex);
            }

            var actionType = sample.ActionType;
            if (string.IsNullOrWhiteSpace(actionType))
            {
                throw new InvalidOperationException(
                    $"Action class '{type.FullName}' returned a null, empty, or whitespace ActionType.");
            }

            if (_actions.ContainsKey(actionType))
            {
                throw new InvalidOperationException(
                    $"Duplicate action name '{actionType}' detected! " +
                    $"Class '{type.FullName}' conflicts with an already registered action.");
            }

            _actions[actionType] = () => (IStepAction)Activator.CreateInstance(type)!;
        }
    }
}
