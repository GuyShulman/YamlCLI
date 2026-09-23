using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Logs a message to the console.
/// Supports ${varName} interpolation in the message field.
/// 
/// YAML usage:
///   - action: log
///     message: "Hello, world!"
/// </summary>
public class LogAction : IStepAction
{
    public string ActionType => "log";

    public Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var message = step.GetRequiredString("message");
        var interpolated = context.Interpolate(message);
        Console.WriteLine($"    {interpolated}");
        return Task.CompletedTask;
    }
}
