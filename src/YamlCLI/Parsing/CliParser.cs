using YamlCLI.Models;

namespace YamlCLI.Parsing;

/// <summary>
/// Parses and validates command-line arguments for the YAML Action Runner CLI.
/// </summary>
public static class CliParser
{
    /// <summary>
    /// Parses an array of command-line arguments into a structured <see cref="CliArguments"/> instance.
    /// </summary>
    public static CliArguments Parse(string[] args)
    {
        var result = new CliArguments();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg.ToLowerInvariant())
            {
                case "--file":
                case "-f":
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        result.FilePath = args[++i];
                    }
                    else
                    {
                        result.Errors.Add("Option '--file' requires a file path argument.");
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

                default:
                    result.Errors.Add($"Unrecognized option: '{arg}'");
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Prints the CLI help and usage information.
    /// </summary>
    public static void PrintHelp(TextWriter? writer = null)
    {
        var outWriter = writer ?? Console.Out;
        outWriter.WriteLine();
        outWriter.WriteLine("  YAML Action Runner CLI");
        outWriter.WriteLine("  ═══════════════════════");
        outWriter.WriteLine();
        outWriter.WriteLine("  A CLI tool that takes a YAML file describing a sequence of steps and runs them.");
        outWriter.WriteLine();
        outWriter.WriteLine("  USAGE:");
        outWriter.WriteLine("    yamlcli --file <path> [options]");
        outWriter.WriteLine();
        outWriter.WriteLine("  OPTIONS:");
        outWriter.WriteLine("    --file, -f <path>    Path to the YAML workflow file (required)");
        outWriter.WriteLine("    --dry-run            Print parsed steps without executing them");
        outWriter.WriteLine("    --verbose, -v        Enable verbose output with execution details");
        outWriter.WriteLine("    --help, -h           Show this help message");
        outWriter.WriteLine();
        outWriter.WriteLine("  EXIT CODES:");
        outWriter.WriteLine("    0    All steps completed successfully");
        outWriter.WriteLine("    1    One or more steps failed or an error occurred");
        outWriter.WriteLine();
        outWriter.WriteLine("  EXAMPLES:");
        outWriter.WriteLine("    yamlcli --file workflow.yaml");
        outWriter.WriteLine("    yamlcli --file workflow.yaml --verbose");
        outWriter.WriteLine("    yamlcli --file workflow.yaml --dry-run");
        outWriter.WriteLine();
        outWriter.WriteLine("  SUPPORTED ACTIONS (11 total):");
        outWriter.WriteLine("    Core Actions:");
        outWriter.WriteLine("      log        Log a message to the console (supports ${var})");
        outWriter.WriteLine("      delay      Wait for a given duration in ms");
        outWriter.WriteLine("      assert     Evaluate a condition expression; fail if false");
        outWriter.WriteLine("      http       Make an HTTP GET or POST request");
        outWriter.WriteLine("      set-var    Set a variable in memory");
        outWriter.WriteLine("      print-var  Print the value of a variable");
        outWriter.WriteLine();
        outWriter.WriteLine("    Advanced Actions:");
        outWriter.WriteLine("      parallel   Execute a list of steps concurrently");
        outWriter.WriteLine("      retry      Retry a step N times on failure with optional delay");
        outWriter.WriteLine("      shell      Execute a shell command, stream and capture output");
        outWriter.WriteLine("      condition  Conditionally branch execution (if/then/else)");
        outWriter.WriteLine("      import     Import and run another YAML workflow file");
        outWriter.WriteLine();
    }
}
