using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Waits for a given duration in milliseconds.
/// 
/// YAML usage:
///   - action: delay
///     duration: 1000
/// </summary>
public class DelayAction : IStepAction
{
    public string ActionType => "delay";

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var duration = step.GetRequiredInt("duration");

        if (duration < 0)
            throw new StepExecutionException($"Delay duration must be non-negative, got {duration}.");

        Console.WriteLine($"    Waiting {duration}ms...");
        await Task.Delay(duration);
    }
}
