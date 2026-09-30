using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Retries a step or a list of steps up to N times if it fails.
/// Supports configurable attempts and optional delay-ms between retries.
/// 
/// YAML usage:
///   - action: retry
///     attempts: 3
///     delay-ms: 500
///     step:
///       action: http
///       url: "https://api.example.com/data"
/// </summary>
public class RetryAction : IStepAction
{
    private static readonly HashSet<string> s_allowedProperties = new(StringComparer.Ordinal)
    {
        "attempts", "delay-ms", "step"
    };

    public string ActionType => "retry";
    public IReadOnlySet<string> AllowedProperties => s_allowedProperties;

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        this.Validate(step);

        var attempts = step.GetOptionalInt("attempts", 3);
        if (attempts <= 0)
            throw new StepExecutionException($"Action 'retry' requires attempts > 0, got {attempts}.");

        var delayMs = step.GetOptionalInt("delay-ms", 0);
        if (delayMs < 0)
            throw new StepExecutionException($"Action 'retry' delay-ms must be non-negative, got {delayMs}.");

        var steps = step.GetSteps("step");
        if (steps.Count == 0)
            throw new StepExecutionException("Action 'retry' requires a 'step' property.");

        if (context.IsDryRun)
        {
            context.Console.Message($"  [retry] Will attempt up to {attempts} times with {delayMs}ms delay");
            foreach (var s in steps)
            {
                await context.ExecuteStepAsync(s);
            }
            return;
        }

        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                if (attempt > 1 && context.IsVerbose)
                {
                    context.Console.Info($"Retry attempt {attempt}/{attempts}...");
                }

                foreach (var childStep in steps)
                {
                    await context.ExecuteStepAsync(childStep);
                }

                // All steps in this attempt succeeded
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < attempts)
                {
                    context.Console.Warning($"Attempt {attempt}/{attempts} failed: {ex.Message}. Retrying...");
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs);
                    }
                }
            }
        }

        throw new StepExecutionException(
            $"Action failed after {attempts} attempts. Last error: {lastException?.Message}",
            lastException!);
    }
}
