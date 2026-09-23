namespace YamlCLI.Models;

/// <summary>
/// Represents a single step parsed from the YAML workflow file.
/// Each step has an action type and action-specific properties stored in a flexible dictionary.
/// </summary>
public class StepDefinition
{
    /// <summary>
    /// The action type identifier (e.g., "log", "delay", "assert", "http", "set-var", "print-var").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Action-specific properties stored as key-value pairs.
    /// Values can be strings, numbers, booleans, lists, or nested dictionaries.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Gets a required string property, throwing if missing.
    /// </summary>
    public string GetRequiredString(string key)
    {
        if (!Properties.TryGetValue(key, out var value))
            throw new StepExecutionException($"Missing required property '{key}' for action '{Action}'.");

        return value?.ToString() ?? throw new StepExecutionException($"Property '{key}' cannot be null for action '{Action}'.");
    }

    /// <summary>
    /// Gets an optional string property, returning null if missing.
    /// </summary>
    public string? GetOptionalString(string key)
    {
        return Properties.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// Gets a required integer property, throwing if missing or not a valid integer.
    /// </summary>
    public int GetRequiredInt(string key)
    {
        if (!Properties.TryGetValue(key, out var value))
            throw new StepExecutionException($"Missing required property '{key}' for action '{Action}'.");

        if (value is int intVal) return intVal;
        if (value is long longVal) return (int)longVal;
        if (int.TryParse(value?.ToString(), out var parsed)) return parsed;

        throw new StepExecutionException($"Property '{key}' must be an integer for action '{Action}', got '{value}'.");
    }

    /// <summary>
    /// Gets an optional integer property, returning the default if missing.
    /// </summary>
    public int GetOptionalInt(string key, int defaultValue = 0)
    {
        if (!Properties.TryGetValue(key, out var value)) return defaultValue;

        if (value is int intVal) return intVal;
        if (value is long longVal) return (int)longVal;
        if (int.TryParse(value?.ToString(), out var parsed)) return parsed;

        return defaultValue;
    }

    /// <summary>
    /// Gets a list of nested step definitions from a property (used by parallel, condition, etc.).
    /// </summary>
    public List<StepDefinition>? GetNestedSteps(string key)
    {
        if (!Properties.TryGetValue(key, out var value)) return null;
        return value as List<StepDefinition>;
    }

    /// <summary>
    /// Gets a single nested step definition from a property (used by retry).
    /// </summary>
    public StepDefinition? GetNestedStep(string key)
    {
        if (!Properties.TryGetValue(key, out var value)) return null;
        return value as StepDefinition;
    }

    /// <summary>
    /// Gets a list of step definitions from a property, accepting either a list or a single step.
    /// Returns an empty list if missing.
    /// </summary>
    public List<StepDefinition> GetSteps(string key)
    {
        if (!Properties.TryGetValue(key, out var value) || value == null)
            return new List<StepDefinition>();

        if (value is List<StepDefinition> list)
            return list;

        if (value is StepDefinition single)
            return new List<StepDefinition> { single };

        return new List<StepDefinition>();
    }

    public override string ToString()
    {
        var props = string.Join(", ", Properties.Select(p => $"{p.Key}={p.Value}"));
        return $"[{Action}] {props}";
    }
}
