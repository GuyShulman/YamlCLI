using YamlCLI.Models;
using YamlCLI.Parsing;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Imports and executes steps from another YAML file into the current execution workflow.
/// File paths can be relative (resolved against the importing file's directory) or absolute.
/// All variables set by imported steps are available in subsequent steps.
/// 
/// YAML usage:
///   - action: import
///     file: "common-setup.yaml"
/// </summary>
public class ImportAction : IStepAction
{
    public string ActionType => "import";

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var rawPath = step.GetOptionalString("file")
            ?? step.GetOptionalString("path");

        if (string.IsNullOrWhiteSpace(rawPath))
            throw new StepExecutionException("Action 'import' requires a 'file' or 'path' property.");

        var filePath = context.Interpolate(rawPath);

        // Resolve relative path against BasePath or current directory
        var resolvedPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(string.IsNullOrEmpty(context.BasePath) ? Directory.GetCurrentDirectory() : context.BasePath, filePath);

        resolvedPath = Path.GetFullPath(resolvedPath);

        if (!File.Exists(resolvedPath))
            throw new StepExecutionException($"Imported YAML file not found: {resolvedPath}");

        if (context.IsDryRun)
        {
            context.Console.Message($"  [import] Loading steps from {filePath}");
        }

        var parser = new YamlParser();
        var importedSteps = parser.ParseFile(resolvedPath);

        context.Console.Info($"Imported {importedSteps.Count} step(s) from {Path.GetFileName(resolvedPath)}");

        // Update BasePath during execution of imported steps to support nested imports
        var prevBasePath = context.BasePath;
        context.BasePath = Path.GetDirectoryName(resolvedPath) ?? prevBasePath;

        try
        {
            foreach (var childStep in importedSteps)
            {
                await context.ExecuteStepAsync(childStep);
            }
        }
        finally
        {
            context.BasePath = prevBasePath;
        }
    }
}
