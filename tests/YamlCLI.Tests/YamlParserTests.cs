using YamlCLI.Parsing;
using YamlCLI.Models;

namespace YamlCLI.Tests;

/// <summary>
/// Tests for the YAML parser - validates parsing of valid/invalid YAML content
/// and correct mapping to StepDefinition objects.
/// </summary>
public class YamlParserTests
{
    private readonly YamlParser _parser = new();

    [Fact]
    public void ParseYaml_ValidLogStep_ParsesCorrectly()
    {
        var yaml = """
            steps:
              - action: log
                message: "Hello"
            """;

        var steps = _parser.ParseYaml(yaml);

        Assert.Single(steps);
        Assert.Equal("log", steps[0].Action);
        Assert.Equal("Hello", steps[0].GetRequiredString("message"));
    }

    [Fact]
    public void ParseYaml_MultipleSteps_ParsesAllSteps()
    {
        var yaml = """
            steps:
              - action: log
                message: "First"
              - action: delay
                duration: 100
              - action: log
                message: "Third"
            """;

        var steps = _parser.ParseYaml(yaml);

        Assert.Equal(3, steps.Count);
        Assert.Equal("log", steps[0].Action);
        Assert.Equal("delay", steps[1].Action);
        Assert.Equal("log", steps[2].Action);
    }

    [Fact]
    public void ParseYaml_SetVarStep_ParsesNameAndValue()
    {
        var yaml = """
            steps:
              - action: set-var
                name: baseUrl
                value: "https://example.com"
            """;

        var steps = _parser.ParseYaml(yaml);

        Assert.Single(steps);
        Assert.Equal("set-var", steps[0].Action);
        Assert.Equal("baseUrl", steps[0].GetRequiredString("name"));
        Assert.Equal("https://example.com", steps[0].GetRequiredString("value"));
    }

    [Fact]
    public void ParseYaml_HttpStep_ParsesAllProperties()
    {
        var yaml = """
            steps:
              - action: http
                method: GET
                url: "https://example.com/api"
                save-status: statusVar
                save-body: bodyVar
            """;

        var steps = _parser.ParseYaml(yaml);

        Assert.Single(steps);
        Assert.Equal("http", steps[0].Action);
        Assert.Equal("GET", steps[0].GetRequiredString("method"));
        Assert.Equal("https://example.com/api", steps[0].GetRequiredString("url"));
        Assert.Equal("statusVar", steps[0].GetOptionalString("save-status"));
        Assert.Equal("bodyVar", steps[0].GetOptionalString("save-body"));
    }

    [Fact]
    public void ParseYaml_DelayStep_ParsesDuration()
    {
        var yaml = """
            steps:
              - action: delay
                duration: 500
            """;

        var steps = _parser.ParseYaml(yaml);

        Assert.Single(steps);
        Assert.Equal("delay", steps[0].Action);
        Assert.Equal(500, steps[0].GetRequiredInt("duration"));
    }

    [Fact]
    public void ParseYaml_AssertStep_ParsesCondition()
    {
        var yaml = """
            steps:
              - action: assert
                condition: "1 + 2 == 3"
            """;

        var steps = _parser.ParseYaml(yaml);

        Assert.Single(steps);
        Assert.Equal("assert", steps[0].Action);
        Assert.Equal("1 + 2 == 3", steps[0].GetRequiredString("condition"));
    }

    [Fact]
    public void ParseYaml_EmptyContent_ThrowsException()
    {
        Assert.Throws<StepExecutionException>(() => _parser.ParseYaml(""));
    }

    [Fact]
    public void ParseYaml_MissingStepsKey_ThrowsException()
    {
        var yaml = """
            actions:
              - type: log
            """;

        Assert.Throws<StepExecutionException>(() => _parser.ParseYaml(yaml));
    }

    [Fact]
    public void ParseYaml_StepWithoutAction_ThrowsException()
    {
        var yaml = """
            steps:
              - message: "No action specified"
            """;

        Assert.Throws<StepExecutionException>(() => _parser.ParseYaml(yaml));
    }

    [Fact]
    public void ParseYaml_ActionIsCaseInsensitive()
    {
        var yaml = """
            steps:
              - action: LOG
                message: "Hello"
            """;

        var steps = _parser.ParseYaml(yaml);

        Assert.Equal("log", steps[0].Action);
    }

    [Fact]
    public void ParseFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => _parser.ParseFile("nonexistent.yaml"));
    }
}
