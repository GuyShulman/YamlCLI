namespace YamlCLI.Models;

/// <summary>
/// Holds the runtime state shared across all step executions.
/// Contains variables, configuration flags, and the console writer for output.
/// </summary>
public class ExecutionContext
{
    /// <summary>
    /// Variables set by 'set-var' actions and available for interpolation and expressions.
    /// </summary>
    public Dictionary<string, object> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// When true, enables verbose output (detailed step execution info).
    /// </summary>
    public bool IsVerbose { get; set; }

    /// <summary>
    /// When true, steps are printed but not executed.
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// Base path used for resolving relative file paths (e.g., for the 'import' action).
    /// Set to the directory containing the input YAML file.
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// Sets a variable in the execution context.
    /// </summary>
    public void SetVariable(string name, object value)
    {
        Variables[name] = value;
    }

    /// <summary>
    /// Gets a variable value, throwing if not found.
    /// </summary>
    public object GetVariable(string name)
    {
        if (!Variables.TryGetValue(name, out var value))
            throw new StepExecutionException($"Variable '{name}' is not defined.");

        return value;
    }

    /// <summary>
    /// Tries to get a variable value, returning false if not found.
    /// </summary>
    public bool TryGetVariable(string name, out object? value)
    {
        return Variables.TryGetValue(name, out value);
    }

    /// <summary>
    /// Interpolates ${varName} references in a string with their variable values.
    /// </summary>
    public string Interpolate(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return System.Text.RegularExpressions.Regex.Replace(input, @"\$\{(\w+)\}", match =>
        {
            var varName = match.Groups[1].Value;
            if (Variables.TryGetValue(varName, out var value))
                return value?.ToString() ?? string.Empty;

            return match.Value; // Leave unresolved references as-is
        });
    }
}
