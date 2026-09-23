namespace YamlCLI.Execution;

/// <summary>
/// Provides colorized console output for the CLI tool.
/// Uses ANSI escape codes for colored status indicators.
/// </summary>
public class ConsoleWriter
{
    /// <summary>
    /// Writes a success message with a green checkmark.
    /// </summary>
    public void Success(string message)
    {
        WriteColored("  ✓ ", ConsoleColor.Green);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes an error message with a red cross.
    /// </summary>
    public void Error(string message)
    {
        WriteColored("  ✗ ", ConsoleColor.Red);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a warning message with a yellow indicator.
    /// </summary>
    public void Warning(string message)
    {
        WriteColored("  ⚠ ", ConsoleColor.Yellow);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes an info message with a cyan indicator (used in verbose mode).
    /// </summary>
    public void Info(string message)
    {
        WriteColored("  ⚡ ", ConsoleColor.Cyan);
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a step header showing step number and action type.
    /// </summary>
    public void StepHeader(int stepNumber, string actionType, bool isDryRun = false)
    {
        var prefix = isDryRun ? "[DRY-RUN] " : "";
        WriteColored($"  ► ", ConsoleColor.DarkCyan);
        Console.WriteLine($"{prefix}Step {stepNumber}: {actionType}");
    }

    /// <summary>
    /// Writes a plain message to the console (for log action output).
    /// </summary>
    public void Message(string message)
    {
        Console.WriteLine($"    {message}");
    }

    /// <summary>
    /// Writes a section header.
    /// </summary>
    public void Header(string message)
    {
        Console.WriteLine();
        WriteColored("═══ ", ConsoleColor.DarkGray);
        Console.Write(message);
        WriteColored(" ═══", ConsoleColor.DarkGray);
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// Writes a summary footer.
    /// </summary>
    public void Summary(int total, int succeeded, int failed, TimeSpan elapsed)
    {
        Console.WriteLine();
        WriteColored("───────────────────────────────────", ConsoleColor.DarkGray);
        Console.WriteLine();

        var color = failed > 0 ? ConsoleColor.Red : ConsoleColor.Green;
        WriteColored($"  Result: ", ConsoleColor.White);
        WriteColored(failed > 0 ? "FAILED" : "PASSED", color);
        Console.WriteLine();

        Console.WriteLine($"  Steps: {succeeded}/{total} succeeded, {failed} failed");
        Console.WriteLine($"  Duration: {elapsed.TotalMilliseconds:F0}ms");

        WriteColored("───────────────────────────────────", ConsoleColor.DarkGray);
        Console.WriteLine();
    }

    private void WriteColored(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }
}
