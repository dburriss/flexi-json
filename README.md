# Flexi.Json

A light, flexible, untyped JSON library for F#, in the shape of
[`FSharp.Data`'s `JsonValue`](https://fsprojects.github.io/FSharp.Data/library/JsonValue.html)
(`JsonValue.Parse`, pattern matching on the cases, `.ToString()`) without the rest of the FSharp.Data ecosystem.

- Strict RFC 8259 parser with line/column errors and a nesting depth limit
- Source positions: the span of every node, keyed by JSON Pointer
- Compact and pretty printer
- Dependency-free (FSharp.Core only), `netstandard2.0` and `net8.0`
- Native AOT safe: no reflection, no `printf`, no regex
- Fable safe: compiles to JavaScript and Python from the same source (the package ships its F# sources under `fable/`)

## Install

```sh
dotnet add package Flexi.Json --prerelease
```

## Usage

### Parse and match

```fsharp
open Flexi.Json

let doc = JsonValue.Parse """{"name":"ann","age":30,"tags":["a","b"]}"""

match doc with
| JsonValue.Record props -> for (key, value) in props do printfn "%s = %O" key value
| _ -> ()
```

The cases are `Null | Boolean of bool | Integer of int64 | Float of float | String of string |
Array of JsonValue[] | Record of (string * JsonValue)[]` (cases must be qualified: `JsonValue.Null`). Integers
that fit in `int64` stay `Integer`; anything with a fraction or exponent, or beyond `int64`, is `Float`. Records keep
property order and duplicate keys.

### Print

```fsharp
doc.ToString()       // {"name":"ann","age":30,"tags":["a","b"]}
doc.ToString(true)   // 2-space pretty printed
```

### Read values

`open Flexi.Json.JsonExtensions` adds accessors that raise if the value is not of the expected shape:

```fsharp
open Flexi.Json.JsonExtensions

doc.["name"].AsString()          // "ann"
doc?age.AsInteger()              // 30 (also AsInteger64, AsFloat, AsDecimal, AsBoolean, AsArray)
doc.TryGetProperty "nope"        // None (also GetProperty, Properties())
doc.TryGetPointer "/tags/1"      // Some (String "b")  (RFC 6901)
doc.["tags"].TryGetItem 5        // None
```

### Positions

```fsharp
let value, spans = JsonValue.ParseWithPositions "{\n  \"a\": [1, 2]\n}"
spans.["/a/1"]   // { Start = { Line = 2; Column = 12; Offset = 13 }; End = { Line = 2; Column = 13; Offset = 14 } }
```

`spans` is a `Map<string, Span>` keyed by JSON Pointer (`""` is the root). `Line` and `Column` are 1-based,
`Offset` is a 0-based index into the source string, and `End` is exclusive. For duplicate keys the first one owns the
pointer. `TryParseWithPositions` returns a `Result` instead of raising.

### Errors

```fsharp
match JsonValue.TryParseResult "{\"a\": }" with
| Error e -> printfn "%s at %d:%d (offset %d)" e.Message e.Pos.Line e.Pos.Column e.Pos.Offset
| Ok _ -> ()
// unexpected character '}' at 1:7 (offset 6)

try JsonValue.Parse "[1,]" |> ignore
with :? JsonParseException as ex -> printfn "%s" ex.Message
// unexpected character ']' (line 1, column 4)
```

`Parse` raises `JsonParseException` (with `Line`, `Column`, `Offset` and the structured `Error`), `TryParseResult`
returns `Result<JsonValue, JsonParseError>`, and `TryParse` returns an option. All take an optional `maxDepth`
(`JsonValue.DefaultMaxDepth`: 512, or 200 under Fable Python).

### Edit

`open Flexi.Json.JsonBuilders` adds immutable edits; each returns a new value:

```fsharp
open Flexi.Json.JsonBuilders

doc.SetProperty("age", JsonValue.Integer 31L).RemoveProperty("tags").ToString()
// {"name":"ann","age":31}
// also RemoveProperty, SetItem(index, v), RemoveItem(index), Append(v)
```

## Fable

Reference the package from a Fable project as usual; Fable compiles it from the sources in `fable/`.
See [docs/fable.md](docs/fable.md) for the numeric and Python differences, and how the test suite runs under Fable.

## Development

```sh
dotnet build -c Release
dotnet test -c Release
scripts/fable-test.sh            # Fable JS and Python (needs bun or node, and Python 3.12+)
./publish.sh --dry-run           # release script (see CHANGELOG.md); tags trigger the publish workflows
```

Publishing an app with Native AOT shows two warnings, IL3053 and IL2104, about `FSharp.Core` itself (its printf and
reflection code, which this library does not call; see [dotnet/fsharp#17324](https://github.com/dotnet/fsharp/issues/17324)).
They are known upstream; silence them with `<NoWarn>IL2104;IL3053</NoWarn>`. Nothing is reported for `Flexi.Json`.

The AOT check lives in `tests/Flexi.Json.AotCheck`:
`dotnet publish tests/Flexi.Json.AotCheck -c Release -r <rid>`.

## License

MIT
