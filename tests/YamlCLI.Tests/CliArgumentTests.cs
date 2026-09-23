namespace YamlCLI.Tests;

/// <summary>
/// Tests for CLI argument parsing logic.
/// </summary>
[Collection("ConsoleTests")]
public class CliArgumentTests
{
    [Fact]
    public void ParseArguments_FileFlag_ParsesFilePath()
    {
        var args = new[] { "--file", "test.yaml" };

        var result = Program.ParseArguments(args);

        Assert.Equal("test.yaml", result.FilePath);
    }

    [Fact]
    public void ParseArguments_ShortFileFlag_ParsesFilePath()
    {
        var args = new[] { "-f", "test.yaml" };

        var result = Program.ParseArguments(args);

        Assert.Equal("test.yaml", result.FilePath);
    }

    [Fact]
    public void ParseArguments_DryRunFlag_SetsFlag()
    {
        var args = new[] { "--file", "test.yaml", "--dry-run" };

        var result = Program.ParseArguments(args);

        Assert.True(result.DryRun);
        Assert.Equal("test.yaml", result.FilePath);
    }

    [Fact]
    public void ParseArguments_VerboseFlag_SetsFlag()
    {
        var args = new[] { "--file", "test.yaml", "--verbose" };

        var result = Program.ParseArguments(args);

        Assert.True(result.Verbose);
    }

    [Fact]
    public void ParseArguments_ShortVerboseFlag_SetsFlag()
    {
        var args = new[] { "--file", "test.yaml", "-v" };

        var result = Program.ParseArguments(args);

        Assert.True(result.Verbose);
    }

    [Fact]
    public void ParseArguments_HelpFlag_SetsFlag()
    {
        var args = new[] { "--help" };

        var result = Program.ParseArguments(args);

        Assert.True(result.ShowHelp);
    }

    [Fact]
    public void ParseArguments_ShortHelpFlag_SetsFlag()
    {
        var args = new[] { "-h" };

        var result = Program.ParseArguments(args);

        Assert.True(result.ShowHelp);
    }

    [Fact]
    public void ParseArguments_AllFlags_ParsesCorrectly()
    {
        var args = new[] { "--file", "workflow.yaml", "--dry-run", "--verbose" };

        var result = Program.ParseArguments(args);

        Assert.Equal("workflow.yaml", result.FilePath);
        Assert.True(result.DryRun);
        Assert.True(result.Verbose);
    }

    [Fact]
    public void ParseArguments_EmptyArgs_NoFilePath()
    {
        var args = Array.Empty<string>();

        var result = Program.ParseArguments(args);

        Assert.Null(result.FilePath);
        Assert.False(result.DryRun);
        Assert.False(result.Verbose);
        Assert.False(result.ShowHelp);
    }

    [Fact]
    public void ParseArguments_CaseInsensitive()
    {
        var args = new[] { "--FILE", "test.yaml", "--DRY-RUN", "--VERBOSE" };

        var result = Program.ParseArguments(args);

        Assert.Equal("test.yaml", result.FilePath);
        Assert.True(result.DryRun);
        Assert.True(result.Verbose);
    }

    [Fact]
    public async Task Main_Help_ReturnsZero()
    {
        var originalOut = Console.Out;
        Console.SetOut(new StringWriter());
        try
        {
            var exitCode = await Program.Main(new[] { "--help" });
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Main_MissingFile_ReturnsOne()
    {
        var originalOut = Console.Out;
        Console.SetOut(new StringWriter());
        try
        {
            var exitCode = await Program.Main(Array.Empty<string>());
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Main_NonExistentFile_ReturnsOne()
    {
        var originalOut = Console.Out;
        Console.SetOut(new StringWriter());
        try
        {
            var exitCode = await Program.Main(new[] { "--file", "nonexistent.yaml" });
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
