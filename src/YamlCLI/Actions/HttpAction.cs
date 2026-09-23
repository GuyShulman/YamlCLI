using YamlCLI.Models;
using ExecutionContext = YamlCLI.Models.ExecutionContext;

namespace YamlCLI.Actions;

/// <summary>
/// Makes an HTTP GET or POST request.
/// Supports optional body for POST requests and saving the response status code and body to variables.
/// 
/// YAML usage:
///   - action: http
///     method: GET
///     url: "https://jsonplaceholder.typicode.com/posts/1"
///     save-status: httpStatus
///     save-body: httpBody
///   
///   - action: http
///     method: POST
///     url: "https://jsonplaceholder.typicode.com/posts"
///     body: '{"title": "foo", "body": "bar", "userId": 1}'
/// </summary>
public class HttpAction : IStepAction
{
    public string ActionType => "http";

    private static readonly HttpClient SharedClient = new();

    public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
    {
        var method = step.GetRequiredString("method").ToUpperInvariant();
        var url = context.Interpolate(step.GetRequiredString("url"));
        var body = step.GetOptionalString("body");
        var saveStatus = step.GetOptionalString("save-status");
        var saveBody = step.GetOptionalString("save-body");

        Console.WriteLine($"    {method} {url}");

        HttpResponseMessage response;

        try
        {
            response = method switch
            {
                "GET" => await SharedClient.GetAsync(url),
                "POST" => await SharedClient.PostAsync(url,
                    body != null
                        ? new StringContent(context.Interpolate(body), System.Text.Encoding.UTF8, "application/json")
                        : null),
                _ => throw new StepExecutionException($"Unsupported HTTP method: {method}. Supported: GET, POST.")
            };
        }
        catch (HttpRequestException ex)
        {
            throw new StepExecutionException($"HTTP request failed: {ex.Message}", ex);
        }

        var statusCode = (int)response.StatusCode;
        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"    Status: {statusCode} {response.StatusCode}");

        if (context.IsVerbose)
        {
            var bodyPreview = responseBody.Length > 200 
                ? responseBody[..200] + "..." 
                : responseBody;
            Console.WriteLine($"    Body: {bodyPreview}");
        }

        // Save response values to variables if requested
        if (!string.IsNullOrEmpty(saveStatus))
        {
            context.SetVariable(saveStatus, statusCode);
            Console.WriteLine($"    Saved status to variable '{saveStatus}'");
        }

        if (!string.IsNullOrEmpty(saveBody))
        {
            context.SetVariable(saveBody, responseBody);
            Console.WriteLine($"    Saved body to variable '{saveBody}'");
        }
    }
}
