namespace YamlCLI.Tests;

/// <summary>
/// Ensures all test classes that redirect Console.Out run serially,
/// preventing race conditions with the shared Console.Out resource.
/// </summary>
[CollectionDefinition("ConsoleTests", DisableParallelization = true)]
public class ConsoleTestsCollection { }
