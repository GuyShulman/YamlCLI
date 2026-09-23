using YamlCLI.Actions;
using YamlCLI.Execution;
using YamlCLI.Parsing;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI;

/// <summary>
/// Entry point for the YAML-Based Action Runner CLI tool.
/// Parses command-line arguments and orchestrates the YAML workflow execution.
/// </summary>
public class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;

    public static async Task<int> Main(string[] args)
    {
        var console = new ConsoleWriter();

        // Parse CLI arguments
        var cliArgs = ParseArguments(args);

        // Handle --help
        if (cliArgs.ShowHelp)
        {
            PrintHelp();
            return ExitSuccess;
        }

        // Validate --file is provided
        if (string.IsNullOrEmpty(cliArgs.FilePath))
        {
            console.Error("Missing required argument: --file <path>");
            Console.WriteLine();
            PrintHelp();
            return ExitFailure;
        }

        // Validate file exists
        if (!File.Exists(cliArgs.FilePath))
        {
            console.Error($"File not found: {cliArgs.FilePath}");
            return ExitFailure;
        }

        try
        {
            // Parse YAML file
            var parser = new YamlParser();
            var steps = parser.ParseFile(cliArgs.FilePath);

            if (steps.Count == 0)
            {
                console.Warning("No steps found in the YAML file.");
                return ExitSuccess;
            }

            // Set up execution context
            var context = new ExecutionContext
            {
                IsVerbose = cliArgs.Verbose,
                IsDryRun = cliArgs.DryRun,
                BasePath = Path.GetDirectoryName(Path.GetFullPath(cliArgs.FilePath)) ?? string.Empty
            };

            // Set up action registry and step runner
            var registry = new ActionRegistry();
            var runner = new StepRunner(registry, console);

            // Run the steps
            var success = await runner.RunAsync(steps, context);
            return success ? ExitSuccess : ExitFailure;
        }
        catch (Models.StepExecutionException ex)
        {
            console.Error(ex.Message);
            return ExitFailure;
        }
        catch (Exception ex)
        {
            console.Error($"Unexpected error: {ex.Message}");
            if (cliArgs.Verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return ExitFailure;
        }
    }

    /// <summary>
    /// Parses command-line arguments into a structured CliArguments object.
    /// </summary>
    public static CliArguments ParseArguments(string[] args)
    {
        var result = new CliArguments();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--file":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        result.FilePath = args[++i];
                    }
                    break;
                case "--dry-run":
                    result.DryRun = true;
                    break;
                case "--verbose":
                case "-v":
                    result.Verbose = true;
                    break;
                case "--help":
                case "-h":
                    result.ShowHelp = true;
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Prints the CLI help/usage information.
    /// </summary>
    private static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("  YAML Action Runner CLI");
        Console.WriteLine("  ═══════════════════════");
        Console.WriteLine();
        Console.WriteLine("  A CLI tool that takes a YAML file describing a sequence of steps and runs them.");
        Console.WriteLine();
        Console.WriteLine("  USAGE:");
        Console.WriteLine("    yamlcli --file <path> [options]");
        Console.WriteLine();
        Console.WriteLine("  OPTIONS:");
        Console.WriteLine("    --file, -f <path>    Path to the YAML workflow file (required)");
        Console.WriteLine("    --dry-run            Print parsed steps without executing them");
        Console.WriteLine("    --verbose, -v        Enable verbose output with execution details");
        Console.WriteLine("    --help, -h           Show this help message");
        Console.WriteLine();
        Console.WriteLine("  EXIT CODES:");
        Console.WriteLine("    0    All steps completed successfully");
        Console.WriteLine("    1    One or more steps failed or an error occurred");
        Console.WriteLine();
        Console.WriteLine("  EXAMPLES:");
        Console.WriteLine("    yamlcli --file workflow.yaml");
        Console.WriteLine("    yamlcli --file workflow.yaml --verbose");
        Console.WriteLine("    yamlcli --file workflow.yaml --dry-run");
        Console.WriteLine();
        Console.WriteLine("  SUPPORTED ACTIONS:");
        Console.WriteLine("    log        Log a message to the console");
        Console.WriteLine("    delay      Wait for a given duration (in ms)");
        Console.WriteLine("    assert     Evaluate a condition; fail if false");
        Console.WriteLine("    http       Make an HTTP GET or POST request");
        Console.WriteLine("    set-var    Set a variable in memory");
        Console.WriteLine("    print-var  Print the value of a variable");
        Console.WriteLine();
    }
}

/// <summary>
/// Represents parsed CLI arguments.
/// </summary>
public class CliArguments
{
    public string? FilePath { get; set; }
    public bool DryRun { get; set; }
    public bool Verbose { get; set; }
    public bool ShowHelp { get; set; }
}
