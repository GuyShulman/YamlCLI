using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Executes a list of steps concurrently using Task.WhenAll.
/// 
/// YAML usage:
///   - action: parallel
///     steps:
///       - action: log
///         message: "Worker 1"
///       - action: log
///         message: "Worker 2"
/// </summary>
public class ParallelAction : IStepAction
{
    private static readonly HashSet<string> s_allowedProperties = new(StringComparer.Ordinal) { "steps" };

    public string ActionType => "parallel";
    public IReadOnlySet<string> AllowedProperties => s_allowedProperties;

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        this.Validate(step);

        var steps = step.GetSteps("steps");

        if (steps.Count == 0)
        {
            context.Console.Warning("Parallel action has no steps to execute.");
            return;
        }

        if (context.IsDryRun)
        {
            context.Console.Message($"  [parallel] {steps.Count} steps configured to run concurrently");
            foreach (var child in steps)
            {
                await context.ExecuteStepAsync(child);
            }
            return;
        }

        context.Console.Info($"Executing {steps.Count} steps in parallel...");

        var tasks = steps.Select(async childStep =>
        {
            await context.ExecuteStepAsync(childStep);
        });

        var allTasks = Task.WhenAll(tasks);
        try
        {
            await allTasks;
        }
        catch (Exception ex)
        {
            var agg = allTasks.Exception ?? (ex as AggregateException);
            var stepExceptions = agg?.InnerExceptions.OfType<StepExecutionException>().ToList() ?? new List<StepExecutionException>();
            if (stepExceptions.Count == 1)
            {
                throw stepExceptions[0];
            }
            if (stepExceptions.Count > 1)
            {
                var messages = string.Join("; ", stepExceptions.Select(e => e.Message));
                throw new StepExecutionException($"Multiple parallel steps failed: {messages}", agg ?? ex);
            }

            throw new StepExecutionException($"One or more parallel steps failed: {agg?.InnerException?.Message ?? ex.Message}", agg ?? ex);
        }
    }
}
