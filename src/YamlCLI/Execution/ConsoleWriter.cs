namespace YamlCLI.Execution;

/// <summary>
/// Provides colorized console output for the CLI tool.
/// Uses ANSI escape codes for colored status indicators.
/// </summary>
public class ConsoleWriter : IConsoleWriter
{
    private readonly TextWriter? _customOut;

    /// <summary>
    /// Initializes a new instance of ConsoleWriter that writes dynamically to Console.Out.
    /// </summary>
    public ConsoleWriter()
    {
    }

    /// <summary>
    /// Initializes a new instance of ConsoleWriter that writes to the specified TextWriter.
    /// If null is passed, defaults to dynamic Console.Out.
    /// </summary>
    /// <param name="output">The target text writer (e.g., StreamWriter for a file, StringWriter for tests).</param>
    public ConsoleWriter(TextWriter? output)
    {
        _customOut = output;
    }

    private TextWriter Out => _customOut ?? Console.Out;

    /// <summary>
    /// Writes a success message with a green checkmark.
    /// </summary>
    public void Success(string message)
    {
        WriteColored("  ✓ ", ConsoleColor.Green);
        Out.WriteLine(message);
    }

    /// <summary>
    /// Writes an error message with a red cross.
    /// </summary>
    public void Error(string message)
    {
        WriteColored("  ✗ ", ConsoleColor.Red);
        Out.WriteLine(message);
    }

    /// <summary>
    /// Writes a warning message with a yellow indicator.
    /// </summary>
    public void Warning(string message)
    {
        WriteColored("  ⚠ ", ConsoleColor.Yellow);
        Out.WriteLine(message);
    }

    /// <summary>
    /// Writes an info message with a cyan indicator (used in verbose mode).
    /// </summary>
    public void Info(string message)
    {
        WriteColored("  ⚡ ", ConsoleColor.Cyan);
        Out.WriteLine(message);
    }

    /// <summary>
    /// Writes a step header showing step number and action type.
    /// </summary>
    public void StepHeader(int stepNumber, string actionType, bool isDryRun = false)
    {
        var prefix = isDryRun ? "[DRY-RUN] " : "";
        WriteColored($"  ► ", ConsoleColor.DarkCyan);
        Out.WriteLine($"{prefix}Step {stepNumber}: {actionType}");
    }

    /// <summary>
    /// Writes a plain message to the console (for log action output).
    /// </summary>
    public void Message(string message)
    {
        Out.WriteLine($"    {message}");
    }

    /// <summary>
    /// Writes a section header.
    /// </summary>
    public void Header(string message)
    {
        Out.WriteLine();
        WriteColored("═══ ", ConsoleColor.DarkGray);
        Out.Write(message);
        WriteColored(" ═══", ConsoleColor.DarkGray);
        Out.WriteLine();
        Out.WriteLine();
    }

    /// <summary>
    /// Writes a summary footer.
    /// </summary>
    public void Summary(int total, int succeeded, int failed, TimeSpan elapsed)
    {
        Out.WriteLine();
        WriteColored("───────────────────────────────────", ConsoleColor.DarkGray);
        Out.WriteLine();

        var color = failed > 0 ? ConsoleColor.Red : ConsoleColor.Green;
        WriteColored($"  Result: ", ConsoleColor.White);
        WriteColored(failed > 0 ? "FAILED" : "PASSED", color);
        Out.WriteLine();

        var executed = succeeded + failed;
        var skipped = total - executed;

        if (skipped > 0)
        {
            Out.WriteLine($"  Steps: {succeeded}/{executed} succeeded, {failed} failed ({skipped} skipped)");
        }
        else
        {
            Out.WriteLine($"  Steps: {succeeded}/{total} succeeded, {failed} failed");
        }

        Out.WriteLine($"  Duration: {elapsed.TotalMilliseconds:F0}ms");

        WriteColored("───────────────────────────────────", ConsoleColor.DarkGray);
        Out.WriteLine();
    }

    private void WriteColored(string text, ConsoleColor color)
    {
        if (_customOut == null || _customOut == Console.Out)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Out.Write(text);
            Console.ForegroundColor = prev;
        }
        else
        {
            _customOut.Write(text);
        }
    }
}
