module PositionTests

open Xunit
open Flexi.Json

[<Fact>]
let ``root and nested spans on multi-line input`` () =
    let text = "{\n  \"a\": [10, {\"b\": \"x\"}],\n  \"c\": true\n}"
    let v, spans = JsonValue.ParseWithPositions text
    Assert.Equal(JsonValue.Parse text, v)
    let at (p: string) = spans.[p]
    // root
    Assert.Equal({ Line = 1; Column = 1; Offset = 0 }, (at "").Start)
    Assert.Equal({ Line = 4; Column = 2; Offset = text.Length }, (at "").End)
    // "a" array starts on line 2 col 8
    Assert.Equal({ Line = 2; Column = 8; Offset = 9 }, (at "/a").Start)
    Assert.Equal(2, (at "/a/0").Start.Line)
    Assert.Equal(9, (at "/a/0").Start.Column)
    Assert.Equal(11, (at "/a/0").End.Column)
    Assert.Equal("{\"b\": \"x\"}", text.Substring((at "/a/1").Start.Offset, (at "/a/1").End.Offset - (at "/a/1").Start.Offset))
    Assert.Equal("\"x\"", text.Substring((at "/a/1/b").Start.Offset, (at "/a/1/b").End.Offset - (at "/a/1/b").Start.Offset))
    Assert.Equal(3, (at "/c").Start.Line)
    Assert.Equal(8, (at "/c").Start.Column)
    Assert.Equal(12, (at "/c").End.Column)
    Assert.Equal(6, spans.Count)

[<Fact>]
let ``pointer keys escape tilde and slash`` () =
    let _, spans = JsonValue.ParseWithPositions """{"a/b":1,"m~n":2,"":3}"""
    Assert.True(spans.ContainsKey "/a~1b")
    Assert.True(spans.ContainsKey "/m~0n")
    Assert.True(spans.ContainsKey "/")

[<Fact>]
let ``duplicate keys first occurrence wins`` () =
    let text = """{"a":1,"a":22}"""
    let _, spans = JsonValue.ParseWithPositions text
    Assert.Equal(5, spans.["/a"].Start.Offset)
    let _, spans2 = JsonValue.ParseWithPositions "{\"a\":{\"x\":1},\"a\":{\"y\":2}}"
    Assert.True(spans2.ContainsKey "/a/x")
    Assert.False(spans2.ContainsKey "/a/y")

[<Fact>]
let ``crlf counts as one line break and bom is not a column`` () =
    let _, spans = JsonValue.ParseWithPositions "\uFEFF[1,\r\n 2]"
    Assert.Equal(1, spans.["/0"].Start.Line)
    Assert.Equal(2, spans.["/0"].Start.Column)
    Assert.Equal(2, spans.["/1"].Start.Line)
    Assert.Equal(2, spans.["/1"].Start.Column)

[<Fact>]
let ``positions parse errors surface through the result api`` () =
    match JsonValue.TryParseWithPositions "[1,\n  oops]" with
    | Error e ->
        Assert.Equal(2, e.Pos.Line)
        Assert.Equal(3, e.Pos.Column)
    | Ok _ -> failwith "expected error"

// ---- ParseWithMetadata / NumberText ----

let private numberText (text: string) = (JsonValue.ParseWithMetadata text).NumberText

[<Fact>]
let ``number text preserves the original spelling`` () =
    let cases =
        [ "1.10"; "1.0"; "2.50"; "1E+2"; "1e-3"; "-0"; "-0.0"; "0"; "1.7976931348623157e308"
          "12345678901234567890"; "-9223372036854775809"; "123456789012345678" ]
    for c in cases do
        let m = numberText c
        Assert.Equal(1, m.Count)
        Assert.Equal(c, m.[""])

[<Fact>]
let ``integers beyond int64 keep exact text while root is a lossy float`` () =
    for c in [ "12345678901234567890"; "-9223372036854775809" ] do
        let parsed = JsonValue.ParseWithMetadata c
        match parsed.Root with
        | JsonValue.Float _ -> ()
        | other -> failwith ("expected Float but got " + other.ToString())
        Assert.Equal(c, parsed.NumberText.[""])

[<Fact>]
let ``number text in arrays and nested objects`` () =
    let m = numberText """{"a":[1.10,{"b":2.50}],"c":{"d":{"e":1E+2}}}"""
    Assert.Equal(3, m.Count)
    Assert.Equal("1.10", m.["/a/0"])
    Assert.Equal("2.50", m.["/a/1/b"])
    Assert.Equal("1E+2", m.["/c/d/e"])

[<Fact>]
let ``number text pointers escape tilde and slash`` () =
    let m = numberText """{"a/b":1.0,"a~b":2.0}"""
    Assert.Equal("1.0", m.["/a~1b"])
    Assert.Equal("2.0", m.["/a~0b"])

[<Fact>]
let ``non-numbers have no number text`` () =
    let m = numberText """{"s":"1.10","t":true,"f":false,"n":null,"x":[],"y":{},"k":3}"""
    Assert.Equal(1, m.Count)
    Assert.Equal("3", m.["/k"])
    Assert.Equal(0, (numberText """{"s":"1.10","t":true,"n":null}""").Count)
    Assert.Equal(0, (numberText "\"1.10\"").Count)
    Assert.Equal(0, (numberText "null").Count)

[<Fact>]
let ``duplicate keys only the first number text is kept`` () =
    let m = numberText """{"a":1.10,"a":2.0}"""
    Assert.Equal(1, m.Count)
    Assert.Equal("1.10", m.["/a"])
    // nothing leaks from a later duplicate's subtree
    let m2 = numberText """{"a":{"x":1.0},"a":{"y":2.0,"x":3.0},"b":4.0}"""
    Assert.Equal(2, m2.Count)
    Assert.Equal("1.0", m2.["/a/x"])
    Assert.False(m2.ContainsKey "/a/y")
    Assert.Equal("4.0", m2.["/b"])
    // a later duplicate does not disturb the first's own text
    let m3 = numberText """{"a":"s","a":5.0}"""
    Assert.Equal(0, m3.Count)

[<Fact>]
let ``number text excludes surrounding whitespace and matches the span`` () =
    let text = " \r\n{ \"a\" :\n  1.10 ,\t\"b\":[ 2E+2\n, -0.0 ] ,\"c\":{\"d\":\n\n12345678901234567890\n} }\n"
    let parsed = JsonValue.ParseWithMetadata text
    Assert.Equal(4, parsed.NumberText.Count)
    Assert.Equal("1.10", parsed.NumberText.["/a"])
    Assert.Equal("2E+2", parsed.NumberText.["/b/0"])
    Assert.Equal("-0.0", parsed.NumberText.["/b/1"])
    for KeyValue(ptr, t) in parsed.NumberText do
        let sp = parsed.Spans.[ptr]
        Assert.Equal(text.Substring(sp.Start.Offset, sp.End.Offset - sp.Start.Offset), t)

[<Fact>]
let ``metadata root and spans equal parse with positions`` () =
    for text in [ "{\n  \"a\": [10, {\"b\": \"x\"}],\n  \"c\": 1.50\n}"; "[1,2.0,{\"a\":1,\"a\":2}]"; "7" ] do
        let v, spans = JsonValue.ParseWithPositions text
        let parsed = JsonValue.ParseWithMetadata text
        Assert.Equal(v, parsed.Root)
        Assert.Equal<Map<string, Span>>(spans, parsed.Spans)

[<Fact>]
let ``metadata errors equal positions errors`` () =
    for bad in [ "[1,\n  oops]"; "{\"a\": }"; ""; "[1.]"; "01"; "{\"a\":1,}" ] do
        let a = JsonValue.TryParseWithPositions bad
        let b = JsonValue.TryParseWithMetadata bad
        match a, b with
        | Error ea, Error eb -> Assert.Equal(ea, eb)
        | _ -> failwith ("expected both to fail for: " + bad)
    match JsonValue.TryParseWithMetadata "[1, 2]" with
    | Ok p -> Assert.Equal(2, p.NumberText.Count)
    | Error _ -> failwith "expected Ok"
