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
    private static readonly HashSet<string> s_allowedProperties = new(StringComparer.Ordinal) { "message" };

    public string ActionType => "log";
    public IReadOnlySet<string> AllowedProperties => s_allowedProperties;

    public Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        this.Validate(step);

        var message = step.GetRequiredString("message");
        var interpolated = context.Interpolate(message);
        context.Console.Message(interpolated);
        return Task.CompletedTask;
    }
}
