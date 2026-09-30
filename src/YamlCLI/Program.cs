using YamlCLI.Actions;
using YamlCLI.Execution;
using YamlCLI.Models;
using YamlCLI.Parsing;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI;

/// <summary>
/// Entry point for the YAML Action Runner CLI tool.
/// Orchestrates CLI parsing, workflow initialization, and execution.
/// </summary>
public class Program
{
    public const int ExitSuccess = 0;
    public const int ExitFailure = 1;

    /// <summary>
    /// Application main method. Bootstraps console output and CLI execution.
    /// </summary>
    public static Task<int> Main(string[] args)
    {
        var console = new ConsoleWriter();
        var cliArgs = ParseArguments(args);
        return RunAsync(cliArgs, console);
    }

    /// <summary>
    /// Runs the CLI lifecycle with injected arguments and console writer.
    /// Allows full end-to-end execution without global console redirection.
    /// </summary>
    public static async Task<int> RunAsync(CliArguments cliArgs, IConsoleWriter console)
    {
        // 1. Handle help display
        if (cliArgs.ShowHelp)
        {
            CliParser.PrintHelp();
            return ExitSuccess;
        }

        // 2. Handle CLI argument errors (unrecognized flags, missing values)
        if (cliArgs.HasErrors)
        {
            foreach (var error in cliArgs.Errors)
            {
                console.Error(error);
            }
            CliParser.PrintHelp();
            return ExitFailure;
        }

        // 3. Validate required --file argument
        if (string.IsNullOrEmpty(cliArgs.FilePath))
        {
            console.Error("Missing required argument: --file <path>");
            CliParser.PrintHelp();
            return ExitFailure;
        }

        // 4. Validate file existence
        if (!File.Exists(cliArgs.FilePath))
        {
            console.Error($"File not found: {cliArgs.FilePath}");
            return ExitFailure;
        }

        // 5. Parse and execute workflow
        try
        {
            return await ExecuteWorkflowAsync(cliArgs, console);
        }
        catch (StepExecutionException ex)
        {
            console.Error(ex.Message);
            return ExitFailure;
        }
        catch (Exception ex)
        {
            console.Error($"Unexpected error: {ex.Message}");
            if (cliArgs.Verbose && !string.IsNullOrEmpty(ex.StackTrace))
            {
                console.Message(ex.StackTrace);
            }
            return ExitFailure;
        }
    }

    /// <summary>
    /// Parses the YAML file, configures execution context, and runs all steps.
    /// </summary>
    private static async Task<int> ExecuteWorkflowAsync(CliArguments cliArgs, IConsoleWriter console)
    {
        var parser = new YamlParser();
        var steps = parser.ParseFile(cliArgs.FilePath!);

        if (steps.Count == 0)
        {
            console.Warning("No steps found in the YAML file.");
            return ExitSuccess;
        }

        var context = new ExecutionContext
        {
            IsVerbose = cliArgs.Verbose,
            IsDryRun = cliArgs.DryRun,
            BasePath = Path.GetDirectoryName(Path.GetFullPath(cliArgs.FilePath!)) ?? string.Empty,
            Console = console
        };

        var registry = new ActionRegistry();
        var runner = new StepRunner(registry, console);

        var success = await runner.RunAsync(steps, context);
        return success ? ExitSuccess : ExitFailure;
    }

    /// <summary>
    /// Forwards argument parsing to <see cref="CliParser"/>.
    /// </summary>
    public static CliArguments ParseArguments(string[] args) => CliParser.Parse(args);
}
