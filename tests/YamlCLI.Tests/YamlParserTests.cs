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

    [Fact]
    public void ParseYaml_StepsNotAList_ThrowsException()
    {
        var yaml = """
            steps: "string-value-not-a-list"
            """;

        var ex = Assert.Throws<StepExecutionException>(() => _parser.ParseYaml(yaml));
        Assert.Contains("The 'steps' key must contain a list of actions", ex.Message);
    }

    [Fact]
    public void ParseYaml_StepNotADictionary_ThrowsException()
    {
        var yaml = """
            steps:
              - "just-a-string-not-a-mapping"
            """;

        var ex = Assert.Throws<StepExecutionException>(() => _parser.ParseYaml(yaml));
        Assert.Contains("Invalid step format", ex.Message);
    }

    [Fact]
    public void ParseYaml_StepActionNull_ThrowsException()
    {
        var yaml = """
            steps:
              - action:
                message: "No action"
            """;

        var ex = Assert.Throws<StepExecutionException>(() => _parser.ParseYaml(yaml));
        Assert.Contains("action", ex.Message);
    }

    [Fact]
    public void ParseYaml_NullPropertyValue_ConvertsToEmptyString()
    {
        var yaml = """
            steps:
              - action: log
                message:
            """;

        var steps = _parser.ParseYaml(yaml);
        Assert.Single(steps);
        Assert.Equal(string.Empty, steps[0].Properties["message"]);
    }

    [Fact]
    public void ParseYaml_RegularListProperty_ConvertsToList()
    {
        var yaml = """
            steps:
              - action: log
                items:
                  - "item1"
                  - "item2"
            """;

        var steps = _parser.ParseYaml(yaml);
        Assert.Single(steps);
        var items = Assert.IsType<List<object>>(steps[0].Properties["items"]);
        Assert.Equal(2, items.Count);
        Assert.Equal("item1", items[0]);
    }

    [Fact]
    public void ParseYaml_RegularDictionaryProperty_ConvertsToDictionary()
    {
        var yaml = """
            steps:
              - action: log
                meta:
                  env: "prod"
                  region: "us-east"
            """;

        var steps = _parser.ParseYaml(yaml);
        Assert.Single(steps);
        var meta = Assert.IsType<Dictionary<string, object>>(steps[0].Properties["meta"]);
        Assert.Equal("prod", meta["env"]);
        Assert.Equal("us-east", meta["region"]);
    }

    [Fact]
    public void ParseYaml_SingleNestedStep_ConvertsToStepDefinition()
    {
        var yaml = """
            steps:
              - action: retry
                step:
                  action: log
                  message: "Nested message"
            """;

        var steps = _parser.ParseYaml(yaml);
        Assert.Single(steps);
        var nested = Assert.IsType<StepDefinition>(steps[0].Properties["step"]);
        Assert.Equal("log", nested.Action);
        Assert.Equal("Nested message", nested.GetRequiredString("message"));
    }

    [Fact]
    public void ParseYaml_NestedStepsList_ParsesAsStepDefinitions()
    {
        var yaml = """
            steps:
              - action: parallel
                steps:
                  - action: log
                    message: "Child 1"
                  - action: delay
                    duration: 50
            """;

        var steps = _parser.ParseYaml(yaml);
        Assert.Single(steps);
        var nestedSteps = Assert.IsType<List<StepDefinition>>(steps[0].Properties["steps"]);
        Assert.Equal(2, nestedSteps.Count);
        Assert.Equal("log", nestedSteps[0].Action);
        Assert.Equal("delay", nestedSteps[1].Action);
    }
}
