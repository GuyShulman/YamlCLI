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

## Bonus Actions

### `parallel` — Run steps in parallel

Executes a list of steps concurrently using `Task.WhenAll`. Shared variables are thread-safe (`ConcurrentDictionary`).

```yaml
- action: parallel
  steps:
    - action: log
      message: "Worker 1 running"
    - action: log
      message: "Worker 2 running"
    - action: delay
      duration: 100
```

| Property | Required | Description |
|---|---|---|
| `steps` | ✓ | List of step definitions to execute in parallel |

### `retry` — Retry on failure

Retries a step or a list of steps up to N times before failing, with optional backoff delay.

```yaml
- action: retry
  attempts: 3
  delay: 500
  step:
    action: http
    url: "https://api.example.com/data"
```

| Property | Required | Description |
|---|---|---|
| `attempts` / `count` / `times` | Optional | Max attempts (default: `3`) |
| `delay` / `delay-ms` | Optional | Delay between retries in milliseconds (default: `0`) |
| `step` / `steps` | ✓ | Single step or list of steps to execute with retry |

### `shell` — Execute shell commands

Executes a command using the system shell (`cmd.exe` on Windows, `/bin/sh` on Unix) and streams standard output and error.

```yaml
- action: shell
  command: "git rev-parse --short HEAD"
  capture-var: gitHash
```

| Property | Required | Description |
|---|---|---|
| `command` | ✓ | Shell command to execute. Supports `${var}` interpolation. |
| `capture-var` | Optional | Context variable name to store standard output |
| `capture-exit-code` | Optional | Context variable name to store process exit code |
| `working-dir` / `cwd` | Optional | Working directory for the process |
| `ignore-error` | Optional | If `true`, does not fail if exit code is non-zero (default: `false`) |

### `condition` — Conditional branching

Evaluates a boolean condition expression and executes the `then` branch if true, or optional `else` branch if false.

```yaml
- action: condition
  if: "${statusCode} == 200"
  then:
    - action: log
      message: "Request succeeded!"
  else:
    - action: log
      message: "Request failed!"
```

| Property | Required | Description |
|---|---|---|
| `if` / `condition` | ✓ | Expression to evaluate. Supports `${var}`, bare variables, and operators. |
| `then` / `steps` | ✓ | Steps to execute when condition is true |
| `else` | Optional | Steps to execute when condition is false |

### `import` — Import and run another YAML workflow

Imports another YAML workflow file into the current execution context. Imported steps share variables with the parent workflow. Relative file paths resolve relative to the importing file's directory.

```yaml
- action: import
  file: "shared/auth-steps.yaml"
```

| Property | Required | Description |
|---|---|---|
| `file` / `path` | ✓ | Path to YAML file. Supports `${var}` interpolation. |

---

## Example Workflows

The `examples/` directory contains sample workflows:

- **[`basic.yaml`](examples/basic.yaml)** — Basic workflow showing log, delay, variables, and assertions.
- **[`http-example.yaml`](examples/http-example.yaml)** — HTTP workflow with GET, POST, response capture, and status assertion.
- **[`bonus-actions.yaml`](examples/bonus-actions.yaml)** — Complete showcase of all 5 bonus actions (shell, condition, parallel, retry, import).
- **[`sub-workflow.yaml`](examples/sub-workflow.yaml)** — Modular sub-workflow imported by `bonus-actions.yaml`.

---

## Tests

The test suite covers all components across 5 test classes:

| Test File | Tests | Coverage |
|---|---|---|
| `YamlParserTests.cs` | 11 | YAML parsing: valid steps, all action types, nested steps, edge cases |
| `ActionTests.cs` | 16 | Core actions: output, timing, HTTP, pass/fail, variables |
| `BonusActionTests.cs` | 23 | All 5 bonus actions: parallel, retry, shell, condition, import, registry |
| `StepRunnerTests.cs` | 7 | Execution order, dry-run, verbose, fail-fast, summary |
| `CliArgumentTests.cs` | 12 | Argument parsing, flags, help, exit codes |
| **Total** | **69** | **100% Passing** |

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

The tool follows the **Strategy Pattern** and **Open-Closed Principle**:

- **`IStepAction`** — Interface that every action type implements (`ActionType` + `ExecuteAsync`)
- **`ActionRegistry`** — Uses reflection to auto-discover all `IStepAction` implementations at startup. Adding a new action is simply creating a class that implements `IStepAction` — zero registration code needed.
- **`StepRunner`** — Iterates through parsed steps, resolves each action from the registry, and executes them with error handling, timing, and formatted output.
- **`ExecutionContext`** — Shared runtime state with thread-safe `ConcurrentDictionary` variables, flags (`IsDryRun`, `IsVerbose`), and step executor helpers.
- **`ConditionEvaluator`** — Unified expression evaluator powered by Dynamic LINQ for both `assert` and `condition` actions.

### Extensibility

To add a new action:

1. Create a new class implementing `IStepAction` in `Actions/`
2. Set `ActionType` to the YAML action name (e.g. `"my-action"`)
3. Implement `ExecuteAsync(StepDefinition step, ExecutionContext context)`

The `ActionRegistry` will automatically discover and register it via reflection.

### Error Handling

- **Fail-fast**: Execution stops on the first error with a clear message and summary
- **Exit codes**: Returns 0 on success, 1 on any failure
- **Colorized output**: Clear visual indicators for steps, dry-run, successes, and failures
