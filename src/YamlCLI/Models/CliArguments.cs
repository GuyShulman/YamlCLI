namespace YamlCLI.Models;

/// <summary>
/// Represents parsed command-line arguments and their validation state.
/// </summary>
public class CliArguments
{
    /// <summary>
    /// Path to the YAML workflow file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// When true, prints parsed steps without executing them.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// When true, enables verbose diagnostic output.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// When true, displays help information.
    /// </summary>
    public bool ShowHelp { get; set; }

    /// <summary>
    /// List of parsing or validation errors encountered.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Indicates whether any errors were encountered during argument parsing.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}
