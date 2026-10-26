module Program

open Flexi.Json

let check (what: string) (ok: bool) =
    if not ok then
        System.Console.Error.WriteLine("FAILED: " + what)
        exit 1

[<EntryPoint>]
let main _ =
    let text = "{\"name\":\"ann\",\"tags\":[1,2.5,true,null]}"

    // parse, match, print
    let v = JsonValue.Parse text
    match v with
    | JsonValue.Record props -> check "two properties" (props.Length = 2)
    | _ -> check "record" false
    check "round trip" (v.ToString() = text)
    check "indented print" (v.ToString(true).Contains "\n")

    // positions
    let _, spans = JsonValue.ParseWithPositions text
    check "span for /tags/0" (spans.ContainsKey "/tags/0")
    check "span line" (spans.["/name"].Start.Line = 1)

    // errors, both shapes
    match JsonValue.TryParseResult "{\"a\":}" with
    | Error e -> check "error position" (e.Pos.Line = 1 && e.Message.Length > 0)
    | Ok _ -> check "error expected" false
    let raised =
        try
            JsonValue.Parse "[1,]" |> ignore
            false
        with :? JsonParseException as ex -> ex.Line = 1
    check "JsonParseException" raised

    System.Console.WriteLine("AOT check OK")
    0
