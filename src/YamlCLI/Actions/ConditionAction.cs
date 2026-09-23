using YamlCLI.Execution;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Conditionally runs steps based on a variable or boolean expression.
/// Supports 'if' / 'condition' for the expression, 'then' / 'steps' for true branch,
/// and optional 'else' for false branch.
/// 
/// YAML usage:
///   - action: condition
///     if: "${status} == 200"
///     then:
///       - action: log
///         message: "Success!"
///     else:
///       - action: log
///         message: "Failed!"
/// </summary>
public class ConditionAction : IStepAction
{
    public string ActionType => "condition";

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var condition = step.GetOptionalString("if")
            ?? step.GetOptionalString("condition");

        if (string.IsNullOrWhiteSpace(condition))
            throw new StepExecutionException("Action 'condition' requires an 'if' or 'condition' property.");

        var thenSteps = step.GetSteps("then");
        if (thenSteps.Count == 0)
            thenSteps = step.GetSteps("steps");

        var elseSteps = step.GetSteps("else");

        if (thenSteps.Count == 0 && elseSteps.Count == 0)
            throw new StepExecutionException("Action 'condition' requires 'then' or 'steps' to execute.");

        if (context.IsDryRun)
        {
            context.Console.Message($"  [condition] If: {condition}");
            context.Console.Message($"    [then] {thenSteps.Count} step(s)");
            if (elseSteps.Count > 0)
            {
                context.Console.Message($"    [else] {elseSteps.Count} step(s)");
            }
            return;
        }

        var evaluated = ConditionEvaluator.Evaluate(condition, context);

        if (context.IsVerbose)
        {
            context.Console.Info($"Condition \"{condition}\" evaluated to {evaluated}");
        }

        if (evaluated)
        {
            foreach (var s in thenSteps)
            {
                await context.ExecuteStepAsync(s);
            }
        }
        else if (elseSteps.Count > 0)
        {
            foreach (var s in elseSteps)
            {
                await context.ExecuteStepAsync(s);
            }
        }
    }
}
