using YamlCLI.Execution;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Conditionally runs steps based on a variable or boolean expression.
/// Strictly requires 'if' for the expression and 'then' for the true branch,
/// with an optional 'else' branch.
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
    private static readonly HashSet<string> s_allowedProperties = new(StringComparer.Ordinal)
    {
        "if", "then", "else"
    };

    public string ActionType => "condition";
    public IReadOnlySet<string> AllowedProperties => s_allowedProperties;

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        this.Validate(step);

        var condition = step.GetRequiredString("if");

        var thenSteps = step.GetSteps("then");
        var elseSteps = step.GetSteps("else");

        if (thenSteps.Count == 0 && elseSteps.Count == 0)
            throw new StepExecutionException("Action 'condition' requires 'then' to execute.");

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
