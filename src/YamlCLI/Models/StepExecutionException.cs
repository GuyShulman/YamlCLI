namespace YamlCLI.Models;

/// <summary>
/// Custom exception thrown when a step fails during execution.
/// </summary>
public class StepExecutionException : Exception
{
    public StepExecutionException(string message) : base(message) { }
    public StepExecutionException(string message, Exception innerException) : base(message, innerException) { }
}
