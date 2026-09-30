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
    private static readonly HashSet<string> s_allowedProperties = new(StringComparer.Ordinal) { "condition" };

    public string ActionType => "assert";
    public IReadOnlySet<string> AllowedProperties => s_allowedProperties;

    public Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        this.Validate(step);

        var condition = step.GetRequiredString("condition");

        context.Console.Message($"Evaluating: {condition}");

        var result = Execution.ConditionEvaluator.Evaluate(condition, context);

        if (result)
        {
            context.Console.Message($"Assertion passed ✓");
        }
        else
        {
            throw new StepExecutionException($"Assertion failed: \"{condition}\" evaluated to false.");
        }

        return Task.CompletedTask;
    }
}
