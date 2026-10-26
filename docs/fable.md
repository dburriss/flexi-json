# Fable support

`Flexi.Json` compiles with [Fable](https://fable.io) 5.21.0 to JavaScript and Python from the same source
(no separate files). The few differences are `#if FABLE_COMPILER` / `#if FABLE_COMPILER_PYTHON` branches in
`src/Flexi.Json/JsonValue.fs`; on .NET every branch is the BCL call the code always made.
The package ships its F# sources and project file under `fable/`, so a plain `PackageReference` works from a
Fable project.

## Running the test suite under Fable

```bash
scripts/fable-test.sh          # JS and Python
scripts/fable-test.sh js
scripts/fable-test.sh python
```

The script exits non-zero when any test fails or the run crashes.

Prerequisites:

- .NET SDK 8+ (Fable is pinned to 5.21.0 in `.config/dotnet-tools.json`; the script runs `dotnet tool restore`)
- JS: `bun` or `node` (anything that runs ES modules)
- Python: 3.12+ (3.13 tested). The script creates `tests/Flexi.Json.Fable/.venv` and installs `fable-library==5.21.0`
  (network needed the first time)

### How it works

xUnit and FsCheck cannot run under Fable, so `tests/Flexi.Json.Fable` compiles the **same test files** as
`tests/Flexi.Json.Tests` through two small shims, `XunitShim.fs` and `FsCheckShim.fs`.
`scripts/fable-gen-runner.fsx` (run by the script, via `dotnet fsi`) reads those files and generates
`Runner.generated.fs` (one entry per `[<Fact>]`, `[<Theory>]` data set or `[<Property>]`) and
`JsonTestSuite.generated.fs` (the vendored JSON Test Suite embedded as strings, since Fable has no file system
access). Generated files are git-ignored. `tests/Flexi.Json.Fable` is not in `Flexi.Json.slnx`.

When you add a test file to `tests/Flexi.Json.Tests`, add the same `<Compile Include=...>` line to
`tests/Flexi.Json.Fable/Flexi.Json.Fable.fsproj`.

All 323 tests run on .NET, Fable JS and Fable Python. `JsonTestSuiteTests.fs` reads files from disk, so
`JsonTestSuiteFable.fs` runs the same y_/n_/i_ checks against the embedded corpus; files that are not valid UTF-8
are stored as "no text" and count as rejected, as in the .NET test.

FsCheck is replaced by a shim: a seeded generator (the same sequence on every target and run, no shrinking), so
the .NET and Fable runs explore different cases.

## Differences from .NET

### Numbers

`Integer of int64` is exact on all three targets (Fable's `int64` is a 64-bit value, not a JS number), including
`Int64.MaxValue`/`MinValue` and `9007199254740993`. Integers outside int64 become `Float`. `-0` is `Integer 0`,
`-0.0` is `Float -0.0` (sign kept). JS consumers see `Long` objects rather than numbers for `int64`.

Float text differs in spelling, not in value: .NET prints `1E+21`, `5E-324`; JS and Python print `1e+21`, `5e-324`.
The output is valid JSON and round-trips on every target; do not compare float text across targets. NaN and
infinities print as `null`.

### Python

- **Recursion depth**: CPython allows about 1000 frames and the parser uses about 2 per level, so
  `JsonValue.DefaultMaxDepth` is 200 under Fable Python (512 elsewhere). Deeper input is a normal
  `JsonParseError`, not a `RecursionError`. Pass `maxDepth` to change it.
- **Strings are code points, not UTF-16 units.** An astral character has `Length` 1, so `Pos.Column` / `Offset`
  count it as one unit (JS and .NET count two). Lone surrogates cannot be represented.
