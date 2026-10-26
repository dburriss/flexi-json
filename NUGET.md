# Flexi.Json

A light, flexible, untyped JSON library for F#, in the shape of `FSharp.Data`'s `JsonValue`.

Parse strict RFC 8259 JSON into a discriminated union you can pattern match, read it with
`AsInteger()`-style accessors, get the source position of every node, and print it back.
No dependencies beyond FSharp.Core, `netstandard2.0`/`net8.0`, Native AOT safe (no reflection, no printf,
no regex), and Fable compatible (JavaScript and Python).

## Install

```sh
dotnet add package Flexi.Json --prerelease
```

## Quick start

```fsharp
open Flexi.Json
open Flexi.Json.JsonExtensions

let doc = JsonValue.Parse """{"name":"ann","age":30,"tags":["a","b"]}"""

match doc with
| JsonValue.Record props -> for (key, value) in props do printfn "%s = %O" key value
| _ -> ()

doc.["name"].AsString()   // "ann"
doc?age.AsInteger()       // 30
doc.ToString(true)        // pretty printed

// where did a node come from?
let _, spans = JsonValue.ParseWithPositions "{\n  \"a\": [1, 2]\n}"
spans.["/a/1"].Start      // { Line = 2; Column = 12; Offset = 13 }

// errors carry a position
match JsonValue.TryParseResult "{\"a\": }" with
| Error e -> printfn "%s at %d:%d" e.Message e.Pos.Line e.Pos.Column
| Ok _ -> ()
```

See the [README](https://github.com/dburriss/flexi-json#readme) for the full API tour.
