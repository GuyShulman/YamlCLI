using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Sets a variable in the execution context.
/// The value supports ${varName} interpolation.
/// 
/// YAML usage:
///   - action: set-var
///     name: baseUrl
///     value: "https://api.example.com"
///   
///   - action: set-var
///     name: fullUrl
///     value: "${baseUrl}/posts"
/// </summary>
public class SetVarAction : IStepAction
{
    private static readonly HashSet<string> s_allowedProperties = new(StringComparer.Ordinal) { "name", "value" };

    public string ActionType => "set-var";
    public IReadOnlySet<string> AllowedProperties => s_allowedProperties;

    public Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        this.Validate(step);

        var name = step.GetRequiredString("name");
        var rawValue = step.GetRequiredString("value");
        var value = context.Interpolate(rawValue);

        context.SetVariable(name, value);
        context.Console.Message($"Set '{name}' = '{value}'");

        return Task.CompletedTask;
    }
}
