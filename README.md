# YAML-Based Action Runner CLI

A C# .NET 10 CLI tool that takes a YAML file describing a sequence of steps and runs them in order.

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build

```bash
dotnet build
```

### Run

```bash
# Run a YAML workflow
dotnet run --project src/YamlCLI -- --file examples/basic.yaml

# Run with verbose output
dotnet run --project src/YamlCLI -- --file examples/basic.yaml --verbose

# Dry-run mode (prints steps without executing)
dotnet run --project src/YamlCLI -- --file examples/basic.yaml --dry-run

# Show help
dotnet run --project src/YamlCLI -- --help
```

### Run Tests

```bash
dotnet test
```

---

## CLI Options

| Option | Description |
|---|---|
| `--file <path>`, `-f <path>` | **(Required)** Path to the YAML workflow file |
| `--dry-run` | Prints parsed steps without executing them |
| `--verbose`, `-v` | Enables verbose output with step execution details |
| `--help`, `-h` | Shows CLI usage and options |

### Exit Codes

| Code | Meaning |
|---|---|
| `0` | All steps completed successfully |
| `1` | One or more steps failed or an error occurred |

---

## YAML Format

Every YAML workflow file has a top-level `steps` key containing a list of actions:

```yaml
steps:
  - action: <action-type>
    <property>: <value>
    ...
```

### Variable Interpolation

Variables set with `set-var` can be referenced in strings using `${varName}` syntax:

```yaml
steps:
  - action: set-var
    name: host
    value: "example.com"
  - action: log
    message: "Connecting to ${host}..."
```

---

## Core Actions

### `log` — Log a message

```yaml
- action: log
  message: "Hello, world!"
```

| Property | Required | Description |
|---|---|---|
| `message` | ✓ | Message to log. Supports `${var}` interpolation. |

### `delay` — Wait for a duration

```yaml
- action: delay
  duration: 1000
```

| Property | Required | Description |
|---|---|---|
| `duration` | ✓ | Time to wait in milliseconds. |

### `assert` — Evaluate a condition

```yaml
- action: assert
  condition: "1 + 2 == 3"
```

| Property | Required | Description |
|---|---|---|
| `condition` | ✓ | Expression to evaluate. Supports arithmetic (`+`, `-`, `*`, `/`), comparisons (`==`, `!=`, `>`, `<`, `>=`, `<=`), and variable references. |

If the condition evaluates to `false`, execution stops immediately with a non-zero exit code.

### `http` — Make an HTTP request

```yaml
- action: http
  method: GET
  url: "https://api.example.com/data"
  save-status: statusCode
  save-body: responseBody
```

| Property | Required | Description |
|---|---|---|
| `method` | ✓ | HTTP method: `GET` or `POST` |
| `url` | ✓ | URL to request. Supports `${var}` interpolation. |
| `body` | | Request body (for POST). Supports `${var}` interpolation. |
| `save-status` | | Variable name to store the response status code (integer). |
| `save-body` | | Variable name to store the response body (string). |

### `set-var` — Set a variable

```yaml
- action: set-var
  name: baseUrl
  value: "https://api.example.com"
```

| Property | Required | Description |
|---|---|---|
| `name` | ✓ | Variable name. |
| `value` | ✓ | Value to store. Supports `${var}` interpolation. |

### `print-var` — Print a variable

```yaml
- action: print-var
  name: baseUrl
```

| Property | Required | Description |
|---|---|---|
| `name` | ✓ | Variable name to print. Throws an error if not defined. |

---

## Tests

The test suite covers all components:

| Test File | Tests | Coverage |
|---|---|---|
| `YamlParserTests.cs` | 11 | YAML parsing: valid steps, all action types, edge cases |
| `ActionTests.cs` | 16 | Each core action: output, timing, pass/fail, variables |
| `StepRunnerTests.cs` | 7 | Execution order, dry-run, verbose, fail-fast, summary |
| `CliArgumentTests.cs` | 12 | Argument parsing, help, exit codes |
| **Total** | **46** | |

Run all tests:

```bash
dotnet test
```

Run with detailed output:

```bash
dotnet test --verbosity normal
```

---

## Design Notes

### Architecture

The tool follows the **Strategy Pattern** for action handling:

- **`IStepAction`** — Interface that every action type implements (`ActionType` + `ExecuteAsync`)
- **`ActionRegistry`** — Uses reflection to auto-discover all `IStepAction` implementations at startup. Adding a new action is simply creating a class that implements `IStepAction` — zero registration code needed.
- **`StepRunner`** — Iterates through parsed steps, resolves each action from the registry, and executes them with error handling.
- **`ExecutionContext`** — Shared runtime state (variables, flags) passed through the execution pipeline.

### Extensibility

To add a new action:

1. Create a new class implementing `IStepAction`
2. Set `ActionType` to the YAML action name
3. Implement `ExecuteAsync` with your logic

That's it — the `ActionRegistry` will auto-discover it via reflection.

### Expression Evaluation

The `assert` action uses **System.Linq.Dynamic.Core** to evaluate expressions at runtime, supporting:

- Arithmetic: `1 + 2 * 3`
- Comparisons: `x > 5`, `status == 200`
- Boolean: `true`, `false`
- Variables are substituted before evaluation

### Error Handling

- **Fail-fast**: Execution stops on the first error with a clear message
- **Exit codes**: Returns 0 on success, 1 on any failure
- **Colorized output**: Green ✓ for success, red ✗ for errors, cyan ⚡ for verbose info
