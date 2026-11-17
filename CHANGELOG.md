# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `JsonParsed`, `JsonValue.ParseWithMetadata` and `JsonValue.TryParseWithMetadata`: return the root, the spans and the original text of every number (`NumberText`, keyed by JSON Pointer), so `1.10`, `1E+2` and integers beyond int64 can be recovered exactly. Existing signatures and behaviour are unchanged.

### Changed

- Targets `netstandard2.0` and `net10.0` instead of `netstandard2.0` and `net8.0`, since .NET 8 went out of support on 2026-11-10. Projects on .NET 8 and 9 use the `netstandard2.0` build.

## [0.1.0] - 2026-10-26

### Added

- First alpha (`0.1.0-alpha.1`): `JsonValue`, extracted from the Fergi project with its namespace renamed from `Fergi.Json` to `Flexi.Json`.
  - Strict RFC 8259 parser (`Parse`, `TryParse`, `TryParseResult`) with line/column errors and a depth limit.
  - Source positions: `ParseWithPositions` / `TryParseWithPositions` return a `Map<string, Span>` keyed by JSON Pointer.
  - Compact and pretty printer (`ToString()`, `ToString(true)`).
  - `JsonExtensions` accessors (`AsString`, `AsInteger`, `TryGetProperty`, `TryGetPointer`, the `?` operator, ...) and `JsonBuilders` immutable edits.
  - Dependency-free, `netstandard2.0` and `net8.0`, Native AOT safe, Fable compatible (JS and Python); the package ships its F# sources under `fable/`.
  - Native AOT publish works; the only warnings are FSharp.Core's collapsed IL2104 and IL3053 (dotnet/fsharp#17324), none from this library.
  - Tested with the vendored JSON Test Suite and property tests, on .NET and under Fable JS and Python.
