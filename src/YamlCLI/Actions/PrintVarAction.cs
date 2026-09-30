using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Prints the value of a previously set variable to the console.
/// 
/// YAML usage:
///   - action: print-var
///     name: baseUrl
/// </summary>
public class PrintVarAction : IStepAction
{
    private static readonly HashSet<string> s_allowedProperties = new(StringComparer.Ordinal) { "name" };

    public string ActionType => "print-var";
    public IReadOnlySet<string> AllowedProperties => s_allowedProperties;

    public Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        this.Validate(step);

        var name = step.GetRequiredString("name");
        var value = context.GetVariable(name);

        context.Console.Message($"{name} = {value}");

        return Task.CompletedTask;
    }
}
