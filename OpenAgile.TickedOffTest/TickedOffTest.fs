namespace OpenAgile.TickedOffTest

open System.Reflection
open NUnit.Framework
open TickSpec

module internal TextReader =
    open System.IO
    /// Reads lines from TextReader
    let ReadLines (reader:System.IO.TextReader) =       
        seq {                  
            let isEOF = ref false
            while not !isEOF do
                let line = reader.ReadLine()                
                if line <> null then yield line
                else isEOF := true
        }
    /// Read all lines to a string array
    let ReadAllLines reader = reader |> ReadLines |> Seq.toArray  
    
[<AutoOpen>]
module Parser =
    let parse source =    
        let ass = Assembly.GetExecutingAssembly()
        let stream = ass.GetManifestResourceStream(source)
        use reader = new System.IO.StreamReader(stream)
        let lines = reader |> TextReader.ReadAllLines
        TickSpec.FeatureParser.parseFeature(lines)

[<AutoOpen>]
module Exceptions =
    exception Pending of unit
    let pending () = raise <| Pending()
    let notImplemented () = raise <| new System.NotImplementedException()

[<AutoOpen>]
module Patterns =
    open System.Text.RegularExpressions

    let Regex input pattern =
        let r = Regex.Match(input,pattern)
        if r.Success then Some [for i = 1 to r.Groups.Count-1 do yield r.Groups.[i].Value]
        else None

    let (|Given|_|) (pattern:string) (step) =
        match step with
        | GivenStep input -> Regex input pattern        
        | WhenStep _ | ThenStep _ -> None

    let (|When|_|) (pattern:string) (step) =
        match step with
        | WhenStep input -> Regex input pattern        
        | GivenStep _ | ThenStep _ -> None    

    let (|Then|_|) (pattern:string) (step) =
        match step with
        | ThenStep input -> Regex input pattern        
        | GivenStep _ | WhenStep _ -> None

    let (|Int|) s = System.Int32.Parse(s)


open System
open System.Net
open System.Net.Http
open Newtonsoft.Json.Linq

[<AutoOpen>]
module Helpers =
  let makeA f a = async { let! x = a
                          return f x }
  let assertStatus s (resp:HttpResponseMessage) = Assert.AreEqual(s, resp.StatusCode)
  let assertBadRequest respA =  respA |> (assertStatus HttpStatusCode.BadRequest |> makeA)
  let assertNotFound respA =  respA |> (assertStatus HttpStatusCode.NotFound |> makeA)

  let getGuid key (o:Newtonsoft.Json.Linq.JObject) = Guid(o.[key].ToString())
  let ignoreA = Async.Ignore
  let await = Async.AwaitTask
  let start = Async.StartAsTask
  let run = Async.RunSynchronously
  let ensureJson (a:Async<HttpResponseMessage>) = async {
    let! resp = a
    let! body = resp.EnsureSuccessStatusCode()
                    .Content
                    .ReadAsStringAsync() |> await
    return JObject.Parse body
    }

module Runner =
    open System

    let tryStep f acc (step,line) =
        let print color =
            let old = Console.ForegroundColor
            Console.ForegroundColor <- color
            printfn "%s" (line.Text.Trim())
            Console.ForegroundColor <- old
        try 
            let acc = f acc (step,line)
            print ConsoleColor.Green
            acc
        with e ->
            print ConsoleColor.Red
            printfn "%A" e
            reraise ()

    let run feature f init =
        let feature = parse feature
        feature.Scenarios
        |> Seq.filter (fun scenario -> scenario.Tags |> Seq.exists ((=) "ignore") |> not) 
        |> Seq.iter (fun scenario ->
            scenario.Steps |> Array.scan (tryStep f) init
            |> ignore
        )
        System.Console.ReadLine () |> ignore
        
    //do run "Addition.txt" (AdditionSteps.performStep) (Calculator.Create ())