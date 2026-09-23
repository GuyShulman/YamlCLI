using System.Diagnostics;
using System.Text;
using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Executes a shell command and logs its output.
/// Supports cross-platform execution (cmd on Windows, sh on Unix),
/// output capturing to context variables, and error code handling.
/// 
/// YAML usage:
///   - action: shell
///     command: "echo Hello from shell"
///   
///   - action: shell
///     command: "git rev-parse --short HEAD"
///     capture-var: gitHash
/// </summary>
public class ShellAction : IStepAction
{
    public string ActionType => "shell";

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var rawCommand = step.GetRequiredString("command");
        var command = context.Interpolate(rawCommand);

        var captureVar = step.GetOptionalString("capture-var")
            ?? step.GetOptionalString("capture");
        var captureExitCode = step.GetOptionalString("capture-exit-code");
        var workingDir = step.GetOptionalString("working-dir")
            ?? step.GetOptionalString("cwd");
        var ignoreError = step.Properties.TryGetValue("ignore-error", out var ie) && Convert.ToBoolean(ie);

        if (context.IsDryRun)
        {
            context.Console.Message($"  [shell] Command: {command}");
            return;
        }

        context.Console.Info($"$ {command}");

        var isWindows = OperatingSystem.IsWindows();
        var fileName = isWindows ? "cmd.exe" : "/bin/sh";
        var arguments = isWindows ? $"/c {command}" : $"-c \"{command.Replace("\"", "\\\"")}\"";

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(workingDir))
        {
            var resolvedDir = Path.IsPathRooted(workingDir)
                ? workingDir
                : Path.Combine(string.IsNullOrEmpty(context.BasePath) ? Directory.GetCurrentDirectory() : context.BasePath, workingDir);
            psi.WorkingDirectory = resolvedDir;
        }

        using var process = new Process { StartInfo = psi };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                outputBuilder.AppendLine(args.Data);
                context.Console.Message(args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                errorBuilder.AppendLine(args.Data);
                context.Console.Message(args.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            throw new StepExecutionException($"Failed to execute shell command '{command}': {ex.Message}", ex);
        }

        var output = outputBuilder.ToString().TrimEnd('\r', '\n');

        if (!string.IsNullOrEmpty(captureVar))
        {
            context.SetVariable(captureVar, output);
        }

        if (!string.IsNullOrEmpty(captureExitCode))
        {
            context.SetVariable(captureExitCode, process.ExitCode);
        }

        if (process.ExitCode != 0 && !ignoreError)
        {
            var err = errorBuilder.ToString().Trim();
            var detail = string.IsNullOrEmpty(err) ? output : err;
            throw new StepExecutionException($"Shell command '{command}' exited with code {process.ExitCode}. {detail}");
        }
    }
}
