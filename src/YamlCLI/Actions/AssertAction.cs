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

        Console.WriteLine($"    Evaluating: {condition}");

        try
        {
            var result = Execution.ConditionEvaluator.Evaluate(condition, context);

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
}
