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
    private static readonly HashSet<string> s_allowedProperties = new(StringComparer.Ordinal) { "duration" };

    public string ActionType => "delay";
    public IReadOnlySet<string> AllowedProperties => s_allowedProperties;

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        this.Validate(step);

        var duration = step.GetRequiredInt("duration");

        if (duration < 0)
            throw new StepExecutionException($"Delay duration must be non-negative, got {duration}.");

        context.Console.Message($"Waiting {duration}ms...");
        await Task.Delay(duration);
    }
}
