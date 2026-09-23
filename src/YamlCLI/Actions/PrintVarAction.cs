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
    public string ActionType => "print-var";

    public Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var name = step.GetRequiredString("name");
        var value = context.GetVariable(name);

        Console.WriteLine($"    {name} = {value}");

        return Task.CompletedTask;
    }
}
