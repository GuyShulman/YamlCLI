using YamlCLI.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YamlCLI.Parsing;

/// <summary>
/// Parses YAML workflow files into a list of StepDefinition objects.
/// Expects a top-level 'steps' key containing a list of action steps.
/// </summary>
public class YamlParser
{
    private readonly IDeserializer _deserializer;

    public YamlParser()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Parses a YAML file at the given path into a list of step definitions.
    /// </summary>
    public List<StepDefinition> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"YAML file not found: {filePath}");

        var yamlContent = File.ReadAllText(filePath);
        return ParseYaml(yamlContent);
    }

    /// <summary>
    /// Parses a YAML string into a list of step definitions.
    /// </summary>
    public List<StepDefinition> ParseYaml(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            throw new StepExecutionException("YAML content is empty.");

        var rawData = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);

        if (rawData == null || !rawData.ContainsKey("steps"))
            throw new StepExecutionException("YAML file must contain a top-level 'steps' key.");

        var stepsRaw = rawData["steps"];
        if (stepsRaw is not List<object> stepsList)
            throw new StepExecutionException("The 'steps' key must contain a list of actions.");

        return stepsList.Select(ParseStep).ToList();
    }

    /// <summary>
    /// Parses a single raw YAML object into a StepDefinition.
    /// </summary>
    private StepDefinition ParseStep(object rawStep)
    {
        if (rawStep is not Dictionary<object, object> stepDict)
            throw new StepExecutionException($"Invalid step format. Each step must be a YAML mapping, got: {rawStep?.GetType().Name ?? "null"}");

        var step = new StepDefinition();

        foreach (var kvp in stepDict)
        {
            var key = kvp.Key.ToString()!;
            var value = kvp.Value;

            if (key == "action")
            {
                step.Action = value?.ToString()?.ToLowerInvariant()
                    ?? throw new StepExecutionException("Step 'action' cannot be null.");
            }
            else
            {
                step.Properties[key] = ConvertValue(key, value);
            }
        }

        if (string.IsNullOrEmpty(step.Action))
            throw new StepExecutionException("Each step must have an 'action' property.");

        return step;
    }

    /// <summary>
    /// Converts a raw YAML value to the appropriate .NET type.
    /// Handles nested steps (lists of mappings) and nested single steps (single mapping with 'action').
    /// </summary>
    private object ConvertValue(string key, object? value)
    {
        return value switch
        {
            null => string.Empty,
            List<object> list => ConvertList(key, list),
            Dictionary<object, object> dict => ConvertDictionary(key, dict),
            _ => value
        };
    }

    /// <summary>
    /// Converts a list value. If list items are mappings with 'action', treats them as nested steps.
    /// </summary>
    private object ConvertList(string key, List<object> list)
    {
        // Check if this is a list of step definitions (each item has an 'action' key)
        if (list.Count > 0 && list[0] is Dictionary<object, object> firstDict && firstDict.ContainsKey("action"))
        {
            return list.Select(ParseStep).ToList();
        }

        // Otherwise, return as a list of converted values
        return list.Select(item => ConvertValue(key, item)).ToList();
    }

    /// <summary>
    /// Converts a dictionary value. If it contains an 'action' key, treats it as a nested step.
    /// </summary>
    private object ConvertDictionary(string key, Dictionary<object, object> dict)
    {
        // If the dictionary has an 'action' key, treat it as a nested step definition
        if (dict.ContainsKey("action"))
        {
            return ParseStep(dict);
        }

        // Otherwise, convert to a string-keyed dictionary
        var result = new Dictionary<string, object>();
        foreach (var kvp in dict)
        {
            result[kvp.Key.ToString()!] = ConvertValue(kvp.Key.ToString()!, kvp.Value);
        }
        return result;
    }
}
