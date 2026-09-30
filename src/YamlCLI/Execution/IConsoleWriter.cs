namespace YamlCLI.Execution;

/// <summary>
/// Interface for console output abstraction.
/// Allows swapping output targets (terminal, file, test capture, etc.)
/// without changing action implementations.
/// </summary>
public interface IConsoleWriter
{
    /// <summary>
    /// Writes a success message with a green checkmark.
    /// </summary>
    void Success(string message);

    /// <summary>
    /// Writes an error message with a red cross.
    /// </summary>
    void Error(string message);

    /// <summary>
    /// Writes a warning message with a yellow indicator.
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// Writes an info message with a cyan indicator (used in verbose mode).
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Writes a step header showing step number and action type.
    /// </summary>
    void StepHeader(int stepNumber, string actionType, bool isDryRun = false);

    /// <summary>
    /// Writes a plain message to the console (for log action output).
    /// </summary>
    void Message(string message);

    /// <summary>
    /// Writes a section header.
    /// </summary>
    void Header(string message);

    /// <summary>
    /// Writes a summary footer.
    /// </summary>
    void Summary(int total, int succeeded, int failed, TimeSpan elapsed);
}
