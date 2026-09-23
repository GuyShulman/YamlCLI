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
    public string ActionType => "parallel";

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var steps = step.GetSteps("steps");
        if (steps.Count == 0)
        {
            steps = step.GetSteps("actions");
        }

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

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            if (ex is AggregateException agg)
            {
                var stepEx = agg.InnerExceptions.OfType<StepExecutionException>().FirstOrDefault();
                if (stepEx != null) throw stepEx;

                throw new StepExecutionException($"One or more parallel steps failed: {agg.InnerException?.Message}", agg);
            }

            throw;
        }
    }
}
