using System.Linq.Dynamic.Core;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Evaluates a condition expression. If the condition evaluates to false,
/// execution stops with a failure.
/// 
/// Supports basic arithmetic, comparisons, and variable references.
/// Variables from the execution context are substituted before evaluation.
/// 
/// YAML usage:
///   - action: assert
///     condition: "1 + 2 == 3"
///   
///   - action: assert
///     condition: "httpStatus == 200"
/// </summary>
public class AssertAction : IStepAction
{
    public string ActionType => "assert";

    public Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var condition = step.GetRequiredString("condition");
        var interpolated = SubstituteVariables(condition, context);

        Console.WriteLine($"    Evaluating: {condition}");

        try
        {
            // Use Dynamic LINQ to evaluate the expression
            var result = EvaluateExpression(interpolated);

            if (result)
            {
                Console.WriteLine($"    Assertion passed ✓");
            }
            else
            {
                throw new StepExecutionException($"Assertion failed: \"{condition}\" evaluated to false.");
            }
        }
        catch (StepExecutionException)
        {
            throw; // Re-throw assertion failures as-is
        }
        catch (Exception ex)
        {
            throw new StepExecutionException($"Failed to evaluate assertion \"{condition}\": {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Substitutes variable names in the expression with their values from the context.
    /// Handles numeric, boolean, and string values appropriately.
    /// </summary>
    private string SubstituteVariables(string expression, ExecutionContext context)
    {
        var result = expression;

        foreach (var kvp in context.Variables)
        {
            var placeholder = kvp.Key;
            var value = kvp.Value;

            // Replace variable references with their values
            // Use word boundary matching to avoid partial replacements
            var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(placeholder)}\b";
            var replacement = FormatValueForExpression(value);
            result = System.Text.RegularExpressions.Regex.Replace(result, pattern, replacement);
        }

        return result;
    }

    /// <summary>
    /// Formats a value for use in an expression string.
    /// </summary>
    private string FormatValueForExpression(object value)
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

    /// <summary>
    /// Evaluates a boolean expression using Dynamic LINQ.
    /// </summary>
    private bool EvaluateExpression(string expression)
    {
        // Create an empty queryable to use Dynamic LINQ's expression parser
        var source = new[] { 0 }.AsQueryable();
        var result = source.Where($"1 == 1 && ({expression})").Any();
        return result;
    }
}
