# Steam Assignment Test
YAML-Based Action Runner CLI

## Overview

Build a C# CLI tool that takes a YAML file describing a sequence of steps and runs them.
The tool should be easy to extend, structured cleanly, and handle execution flow correctly.

## Requirements

A CLI tool written in C# .NET 10 that:

* Accepts a YAML file describing steps to execute
* Parses and runs the steps in order
* Includes at least the core actions defined below
* Handles errors and gives user-friendly output
* Is extensible to support more actions later

## Required Features

Your CLI tool must support:

| Feature | Description |
| :--- | :--- |
| –file <path> (required) | path to the yaml file |
| –dry-run | prints the parsed steps but does not execute them |
| –verbose | enables verbose output (show each step execution details) |
| –help | shows cli usage and options |
| Exit codes | Should return non-zero on failure or assertion errors |

## Actions:

Your YAML must support these actions. Design the YAML structure yourself.

| Action Type | Description |
| :--- | :--- |
| log | Logs a message to the console. Must support at least message. |
| delay | Waits for a given duration (in ms). Must support a duration field. |
| assert | Evaluates a condition. If false, the execution should stop or fail. The condition can be a string like "1 + 2 == 3". |
| http | Make a simple HTTP GET or POST request. At minimum, should support method, url, body (optional) |
| set-var | Sets a variable in memory (e.g., to be used in later steps). Support variable name and value. |
| print-var | Logs the value of a previously set variable. |

Note: How variables work (syntax, scope, reference) is entirely up to you.

## Bonus Actions:

You can implement any of the following for bonus points (the implementation design is up to you):

| Bonus Action | Description |
| :--- | :--- |
| parallel | accepts a list of steps to run in parallel |
| retry | retry a step N times if fails |
| shell | execute shell command and logs output |
| condition | Conditionally runs steps based on a variable or expression |
| import | Supports loading another YAML file into the current |

## Deliverables

Please submit:

* Source code
* Example YAML file(s)
* README with:
  * How to build and run the CLI
  * How your YAML format works (since you define it)
  * Tests
* Notes about your design (optional)

Good Luck!
