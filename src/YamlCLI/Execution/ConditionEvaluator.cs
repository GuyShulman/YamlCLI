using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Execution;

/// <summary>
/// Evaluates conditional expressions and assertions using Dynamic LINQ.
/// Supports variable interpolation, arithmetic, comparisons, and boolean logic.
/// </summary>
public static class ConditionEvaluator
{
    /// <summary>
    /// Evaluates a condition string against the provided execution context.
    /// Returns true or false.
    /// </summary>
    public static bool Evaluate(string condition, ExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(condition))
            throw new StepExecutionException("Condition expression cannot be empty.");

        // First interpolate ${var} syntax
        var interpolated = context.Interpolate(condition);

        // Substitute bare variable names with context values
        var prepared = SubstituteVariables(interpolated, context);

        try
        {
            var source = new[] { 0 }.AsQueryable();
            return source.Where($"1 == 1 && ({prepared})").Any();
        }
        catch (Exception ex)
        {
            throw new StepExecutionException($"Failed to evaluate condition \"{condition}\": {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Substitutes variable names in the expression with their values from the context.
    /// Handles numeric, boolean, and string values appropriately.
    /// </summary>
    private static string SubstituteVariables(string expression, ExecutionContext context)
    {
        var result = expression;

        foreach (var kvp in context.Variables)
        {
            var placeholder = kvp.Key;
            var value = kvp.Value;

            // Replace variable references with their values using word boundary matching
            var pattern = $@"\b{Regex.Escape(placeholder)}\b";
            var replacement = FormatValueForExpression(value);
            result = Regex.Replace(result, pattern, replacement);
        }

        return result;
    }

    /// <summary>
    /// Formats a value for use in a Dynamic LINQ expression string.
    /// </summary>
    private static string FormatValueForExpression(object value)
    {
        return value switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            int or long or float or double or decimal => value.ToString()!,
            string s when int.TryParse(s, out _) => s,
            string s when double.TryParse(s, out _) => s,
            string s when bool.TryParse(s, out var b) => b.ToString().ToLowerInvariant(),
            string s => $"\"{s}\"",
            _ => $"\"{value}\""
        };
    }
}
