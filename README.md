# YAML-Based Action Runner CLI

A robust, extensible C# .NET 10 CLI tool that parses YAML workflow files and executes steps sequentially or concurrently, featuring variable interpolation, dynamic expression evaluation, resilient error handling, and modular sub-workflow composition.

---

## Table of Contents

- [Quick Start](#quick-start)
  - [Prerequisites](#prerequisites)
  - [Build](#build)
  - [Run](#run)
- [CLI Options](#cli-options)
  - [Exit Codes](#exit-codes)
- [YAML Workflow Format](#yaml-workflow-format)
  - [Variable Interpolation](#variable-interpolation)
- [Supported Actions](#supported-actions)
  - [Core Actions](#core-actions)
    - [1. `log`](#1-log--log-a-message)
    - [2. `delay`](#2-delay--wait-duration)
    - [3. `assert`](#3-assert--evaluate-condition)
    - [4. `http`](#4-http--http-request)
    - [5. `set-var`](#5-set-var--set-variable)
    - [6. `print-var`](#6-print-var--print-variable)
  - [Bonus Actions](#bonus-actions)
    - [7. `parallel`](#7-parallel--concurrent-execution)
    - [8. `retry`](#8-retry--resilient-retries)
    - [9. `shell`](#9-shell--execute-system-commands)
    - [10. `condition`](#10-condition--conditional-branching)
    - [11. `import`](#11-import--modular-workflow-reuse)
- [Example Workflows](#example-workflows)
- [Testing & Code Coverage](#testing--code-coverage)
  - [Running Tests](#running-tests)
  - [Viewing Code Coverage in Terminal](#viewing-code-coverage-in-terminal)
  - [Coverage Breakdown](#coverage-breakdown)
- [Design & Architecture Notes](#design--architecture-notes)
  - [Extensibility (Adding New Actions)](#extensibility-adding-new-actions)
  - [Fail-Fast Execution Flow](#fail-fast-execution-flow)

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (v10.0 or higher)

### Build

```bash
dotnet build
```

### Run

```bash
# Run a basic workflow
dotnet run --project src/YamlCLI -- --file examples/basic.yaml

# Run with verbose output (shows timing and internal details)
dotnet run --project src/YamlCLI -- --file examples/basic.yaml --verbose

# Dry-run mode (parses and validates steps without execution)
dotnet run --project src/YamlCLI -- --file examples/basic.yaml --dry-run

# Show help menu
dotnet run --project src/YamlCLI -- --help
```

---

## CLI Options

| Option | Shorthand | Description |
|---|---|---|
| `--file <path>` | `-f <path>` | **(Required)** Path to the YAML workflow file |
| `--dry-run` | | Validates and displays parsed steps without executing them |
| `--verbose` | `-v` | Enables detailed step execution logs and millisecond timers |
| `--help` | `-h` | Displays CLI usage instructions and available actions |

### Exit Codes

| Exit Code | Meaning |
|---|---|
| `0` | All executed steps succeeded |
| `1` | One or more steps failed, an assertion failed, or a file/parsing error occurred |

---

## YAML Workflow Format

Every workflow file contains a top-level `steps` list:

```yaml
steps:
  - action: <action-name>
    <property>: <value>
```

### Variable Interpolation

Variables stored in the execution context can be referenced inside strings using `${varName}`:

```yaml
steps:
  - action: set-var
    name: greeting
    value: "Hello, World!"

  - action: log
    message: "Message is: ${greeting}"
```

Variables are case-insensitive and thread-safe.

---

## Supported Actions

### Core Actions

#### 1. `log` — Log a message

Prints a message to the console with variable interpolation.

```yaml
- action: log
  message: "Current endpoint: ${baseUrl}"
```

| Property | Required | Type | Description |
|---|---|---|---|
| `message` | ✓ | `string` | Text to display. Supports `${var}` interpolation. |

#### 2. `delay` — Wait duration

Asynchronously pauses execution without blocking threads.

```yaml
- action: delay
  duration: 500
```

| Property | Required | Type | Description |
|---|---|---|---|
| `duration` | ✓ | `integer` | Wait duration in milliseconds. Must be $\ge 0$. |

#### 3. `assert` — Evaluate condition

Evaluates an arithmetic, comparison, or boolean expression. Execution stops if the assertion evaluates to `false`.

```yaml
- action: assert
  condition: "1 + 2 == 3"

- action: assert
  condition: "${statusCode} == 200"
```

| Property | Required | Type | Description |
|---|---|---|---|
| `condition` | ✓ | `string` | Expression to evaluate using Dynamic LINQ (e.g. `==`, `!=`, `<`, `>`, `<=`, `>=`, `&&`, `\|\|`). |

#### 4. `http` — HTTP request

Sends an HTTP `GET` or `POST` request with JSON support and variable capturing.

```yaml
- action: http
  method: GET
  url: "https://jsonplaceholder.typicode.com/posts/1"
  save-status: postStatus
  save-body: postBody

- action: http
  method: POST
  url: "https://jsonplaceholder.typicode.com/posts"
  body: '{"title": "foo", "body": "bar", "userId": 1}'
  save-status: createStatus
```

| Property | Required | Type | Description |
|---|---|---|---|
| `method` | ✓ | `string` | HTTP method: `GET` or `POST`. |
| `url` | ✓ | `string` | Target endpoint. Supports `${var}` interpolation. |
| `body` | Optional | `string` | JSON payload for `POST` requests. |
| `save-status` | Optional | `string` | Context variable to save the integer HTTP status code. |
| `save-body` | Optional | `string` | Context variable to save the response body string. |

#### 5. `set-var` — Set variable

Stores a variable in memory for subsequent steps.

```yaml
- action: set-var
  name: apiUrl
  value: "https://api.example.com"
```

| Property | Required | Type | Description |
|---|---|---|---|
| `name` | ✓ | `string` | Variable name. |
| `value` | ✓ | `any` | Value to store. Supports `${var}` interpolation. |

#### 6. `print-var` — Print variable

Outputs the current value of a variable stored in context.

```yaml
- action: print-var
  name: apiUrl
```

| Property | Required | Type | Description |
|---|---|---|---|
| `name` | ✓ | `string` | Name of the variable to display. Throws if not defined. |

---

### Bonus Actions

#### 7. `parallel` — Concurrent execution

Runs multiple child steps in parallel using `Task.WhenAll`. Shared variables are backed by a thread-safe `ConcurrentDictionary`.

```yaml
- action: parallel
  steps:
    - action: log
      message: "Worker A starting"
    - action: log
      message: "Worker B starting"
    - action: delay
      duration: 200
```

| Property | Required | Type | Description |
|---|---|---|---|
| `steps` | ✓ | `list` | List of step definitions to execute concurrently. |

#### 8. `retry` — Resilient retries

Retries a step or a list of steps up to $N$ times upon failure with optional delay.

```yaml
- action: retry
  attempts: 3
  delay: 500
  step:
    action: http
    method: GET
    url: "https://api.example.com/health"
```

| Property | Required | Type | Description |
|---|---|---|---|
| `attempts` / `count` / `times` | Optional | `integer` | Max attempts (default: `3`). |
| `delay` / `delay-ms` | Optional | `integer` | Wait duration in milliseconds between retries (default: `0`). |
| `step` / `steps` | ✓ | `step` or `list` | Step(s) to execute with retry protection. |

#### 9. `shell` — Execute system commands

Executes a command using the system shell (`cmd.exe` on Windows, `/bin/sh` on Unix) and captures standard output and exit codes.

```yaml
- action: shell
  command: "git rev-parse --short HEAD"
  capture-var: gitHash
  capture-exit-code: gitExit
```

| Property | Required | Type | Description |
|---|---|---|---|
| `command` | ✓ | `string` | Shell command to execute. Supports `${var}` interpolation. |
| `capture-var` | Optional | `string` | Variable name to store standard output. |
| `capture-exit-code` | Optional | `string` | Variable name to store exit code. |
| `working-dir` / `cwd` | Optional | `string` | Working directory for command execution. |
| `ignore-error` | Optional | `boolean` | If `true`, does not fail if exit code is non-zero (default: `false`). |

#### 10. `condition` — Conditional branching

Evaluates expressions and runs the `then` branch if true, or the optional `else` branch if false.

```yaml
- action: condition
  if: "${statusCode} == 200"
  then:
    - action: log
      message: "Request succeeded!"
  else:
    - action: log
      message: "Request failed with code ${statusCode}"
```

| Property | Required | Type | Description |
|---|---|---|---|
| `if` / `condition` | ✓ | `string` | Expression to evaluate. |
| `then` / `steps` | ✓ | `step` or `list` | Step(s) to execute if condition is true. |
| `else` | Optional | `step` or `list` | Step(s) to execute if condition is false. |

#### 11. `import` — Modular workflow reuse

Imports and executes steps from an external YAML workflow file within the current context. Relative paths resolve against the importing YAML file's directory.

```yaml
- action: import
  file: "sub-workflow.yaml"
```

| Property | Required | Type | Description |
|---|---|---|---|
| `file` / `path` | ✓ | `string` | Path to target YAML workflow file. |

---

## Example Workflows

The `examples/` directory contains sample workflows ready to run:

| Workflow File | Description | Command |
|---|---|---|
| **[`basic.yaml`](examples/basic.yaml)** | Core workflow demonstration (variables, delays, assertions) | `dotnet run --project src/YamlCLI -- --file examples/basic.yaml` |
| **[`http-example.yaml`](examples/http-example.yaml)** | HTTP GET/POST calls, JSON payload, and status assertions | `dotnet run --project src/YamlCLI -- --file examples/http-example.yaml` |
| **[`bonus-actions.yaml`](examples/bonus-actions.yaml)** | Complete showcase of all 5 bonus actions in action | `dotnet run --project src/YamlCLI -- --file examples/bonus-actions.yaml` |
| **[`sub-workflow.yaml`](examples/sub-workflow.yaml)** | Modular sub-workflow imported by `bonus-actions.yaml` | (Imported automatically) |

---

## Testing & Code Coverage

The solution contains **82 automated tests** across 5 test classes with **100% pass rate**:

| Test Class | Tests | Focus Area |
|---|---|---|
| [`YamlParserTests.cs`](tests/YamlCLI.Tests/YamlParserTests.cs) | 11 | Valid/invalid YAML structures, nested steps, missing keys |
| [`ActionTests.cs`](tests/YamlCLI.Tests/ActionTests.cs) | 23 | Core actions in isolation (`log`, `delay`, `assert`, `http` GET/POST, `set-var`, `print-var`) |
| [`BonusActionTests.cs`](tests/YamlCLI.Tests/BonusActionTests.cs) | 23 | Bonus actions (`parallel`, `retry`, `shell`, `condition`, `import`, registry discovery) |
| [`StepRunnerTests.cs`](tests/YamlCLI.Tests/StepRunnerTests.cs) | 8 | Sequential execution, fail-fast on error, dry-run, verbose output, summary reporting |
| [`CliArgumentTests.cs`](tests/YamlCLI.Tests/CliArgumentTests.cs) | 17 | CLI argument parsing, flags, help menu, exit codes, `Program.Main` execution |
| **Total** | **82** | **100% Passing** |

### Running Tests

```bash
# Standard test run
dotnet test

# Detailed test output
dotnet test --verbosity normal
```

### Viewing Code Coverage in Terminal

Run the included coverage script to inspect code coverage with a colorized, per-component breakdown:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/coverage.ps1
```

**Terminal Output Preview:**
```text
===========================================================================
Class / Component                   |  Total Lines |    Covered |   Coverage %
---------------------------------------------------------------------------
Actions.ActionRegistry              |           34 |         31 |        91.2%
Actions.AssertAction                |           21 |         18 |        85.7%
Actions.ConditionAction             |           41 |         28 |        68.3%
Actions.DelayAction                 |            8 |          8 |       100.0%
Actions.HttpAction                  |           51 |         51 |       100.0%
Actions.ImportAction                |           32 |         29 |        90.6%
Actions.LogAction                   |            7 |          7 |       100.0%
Actions.ParallelAction              |           36 |         32 |        88.9%
Actions.PrintVarAction              |            7 |          7 |       100.0%
Actions.RetryAction                 |           61 |         44 |        72.1%
Actions.SetVarAction                |            9 |          9 |       100.0%
Actions.ShellAction                 |           77 |         61 |        79.2%
CliArguments                        |            4 |          4 |       100.0%
Execution.ConditionEvaluator        |           36 |         30 |        83.3%
Execution.ConsoleWriter             |           59 |         59 |       100.0%
Execution.StepRunner                |           70 |         61 |        87.1%
Models.ExecutionContext             |           48 |         44 |        91.7%
Models.StepDefinition               |           46 |         33 |        71.7%
Models.StepExecutionException       |            2 |          2 |       100.0%
Parsing.YamlParser                  |           70 |         48 |        68.6%
Program                             |          108 |        104 |        96.3%
===========================================================================
OVERALL LINE COVERAGE   : 85.8% (710/827 lines)
OVERALL BRANCH COVERAGE : 73.1%
===========================================================================
```

---

## Design & Architecture Notes

### Strategy Pattern & Reflection Discovery

- **`IStepAction`**: Unified interface implemented by all actions (`ActionType` identifier + `ExecuteAsync` method).
- **`ActionRegistry`**: Scans the executing assembly via reflection at startup. New actions require zero registration boilerplate.
- **`ExecutionContext`**: Thread-safe runtime state using `ConcurrentDictionary<string, object>`.
- **`ConditionEvaluator`**: Centralized Dynamic LINQ expression evaluator shared by `assert` and `condition`.

### Extensibility (Adding New Actions)

To add a custom action:

1. Create a class implementing `IStepAction` in `src/YamlCLI/Actions/`:
   ```csharp
   public class MyCustomAction : IStepAction
   {
       public string ActionType => "my-custom";

       public async Task ExecuteAsync(StepDefinition step, ExecutionContext context)
       {
           var message = step.GetRequiredString("message");
           context.Console.Message($"Custom: {message}");
       }
   }
   ```
2. Build the project. The action is immediately available in YAML files:
   ```yaml
   steps:
     - action: my-custom
       message: "Works out of the box!"
   ```

### Fail-Fast Execution Flow

If any step throws an error or assertion failure, execution stops immediately. The runner reports the exact count of succeeded, failed, and skipped steps:

```text
───────────────────────────────────
  Result: FAILED
  Steps: 5/6 succeeded, 1 failed (1 skipped)
  Duration: 2156ms
───────────────────────────────────
```
