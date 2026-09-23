using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Retries a step or a list of steps up to N times if it fails.
/// Supports configurable attempts and optional delay between retries.
/// 
/// YAML usage:
///   - action: retry
///     attempts: 3
///     delay: 500
///     step:
///       action: http
///       url: "https://api.example.com/data"
/// 
///   - action: retry
///     count: 3
///     steps:
///       - action: log
///         message: "Trying..."
/// </summary>
public class RetryAction : IStepAction
{
    public string ActionType => "retry";

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var attempts = 3;
        if (step.Properties.ContainsKey("attempts"))
            attempts = step.GetRequiredInt("attempts");
        else if (step.Properties.ContainsKey("count"))
            attempts = step.GetRequiredInt("count");
        else if (step.Properties.ContainsKey("times"))
            attempts = step.GetRequiredInt("times");
        else if (step.Properties.ContainsKey("retries"))
            attempts = step.GetRequiredInt("retries") + 1;

        if (attempts <= 0)
            throw new StepExecutionException($"Action 'retry' requires attempts > 0, got {attempts}.");

        var delayMs = step.GetOptionalInt("delay", 0);
        if (delayMs == 0 && step.Properties.ContainsKey("delay-ms"))
            delayMs = step.GetRequiredInt("delay-ms");

        var steps = step.GetSteps("steps");
        if (steps.Count == 0)
        {
            var single = step.GetNestedStep("step");
            if (single != null)
            {
                steps.Add(single);
            }
        }

        if (steps.Count == 0)
            throw new StepExecutionException("Action 'retry' requires a 'step' or 'steps' property.");

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
