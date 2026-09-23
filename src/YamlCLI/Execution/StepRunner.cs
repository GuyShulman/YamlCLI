using System.Diagnostics;
using YamlCLI.Actions;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Execution;

/// <summary>
/// Executes a list of step definitions in order, resolving each action from the registry.
/// Handles dry-run mode, verbose output, and fail-fast error handling.
/// </summary>
public class StepRunner
{
    private readonly ActionRegistry _registry;
    private readonly ConsoleWriter _console;

    public StepRunner(ActionRegistry registry, ConsoleWriter console)
    {
        _registry = registry;
        _console = console;
    }

    /// <summary>
    /// Runs all steps in the given list sequentially.
    /// Returns true if all steps succeeded, false if any step failed.
    /// </summary>
    public async Task<bool> RunAsync(List<StepDefinition> steps, ExecutionContext context)
    {
        context.Registry = _registry;
        context.Console = _console;

        _console.Header("YAML Action Runner");

        var totalSteps = steps.Count;
        var succeeded = 0;
        var failed = 0;
        var overallStopwatch = Stopwatch.StartNew();

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var stepNumber = i + 1;

            try
            {
                _console.StepHeader(stepNumber, step.Action, context.IsDryRun);

                if (context.IsDryRun)
                {
                    PrintDryRunInfo(step);
                    succeeded++;
                    continue;
                }

                var action = _registry.GetAction(step.Action);

                if (context.IsVerbose)
                {
                    _console.Info($"Executing {step.Action} with properties: {step}");
                }

                var stepStopwatch = Stopwatch.StartNew();
                await action.ExecuteAsync(step, context);
                stepStopwatch.Stop();

                if (context.IsVerbose)
                {
                    _console.Info($"Completed in {stepStopwatch.ElapsedMilliseconds}ms");
                }

                _console.Success($"{step.Action} completed");
                succeeded++;
            }
            catch (StepExecutionException ex)
            {
                _console.Error($"{step.Action} failed: {ex.Message}");
                failed++;
                overallStopwatch.Stop();
                _console.Summary(totalSteps, succeeded, failed, overallStopwatch.Elapsed);
                return false; // Fail-fast
            }
            catch (Exception ex)
            {
                _console.Error($"{step.Action} failed with unexpected error: {ex.Message}");
                failed++;
                overallStopwatch.Stop();
                _console.Summary(totalSteps, succeeded, failed, overallStopwatch.Elapsed);
                return false; // Fail-fast
            }
        }

        overallStopwatch.Stop();
        _console.Summary(totalSteps, succeeded, failed, overallStopwatch.Elapsed);
        return true;
    }

    /// <summary>
    /// Prints step details in dry-run mode without executing.
    /// </summary>
    private void PrintDryRunInfo(StepDefinition step)
    {
        foreach (var prop in step.Properties)
        {
            var valueStr = prop.Value switch
            {
                List<StepDefinition> nestedSteps => $"[{nestedSteps.Count} nested step(s)]",
                StepDefinition nestedStep => $"[nested: {nestedStep.Action}]",
                _ => prop.Value?.ToString() ?? "null"
            };
            _console.Message($"  {prop.Key}: {valueStr}");
        }
    }
}
