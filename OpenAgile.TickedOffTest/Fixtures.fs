namespace OpenAgile.TickedOffTest

open NUnit.Framework
open System.IO
open System.Reflection
open TickSpec
open OpenAgile.TickedOffTest

[<AbstractClass>]
[<TestFixture>]
type FeatureFixture<'St>(featureFile, getZero : unit -> 'St, perform : 'St -> StepSource -> 'St, ass : Assembly) = 
    //let assembly = Assembly.GetExecutingAssembly() 
    let definitions = new StepDefinitions(ass)

    let feature = parse featureFile ass

    let notIgnored (scenario:ScenarioSource) =
        scenario.Tags |> Seq.exists ((=) "ignore") |> not

    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member this.TestScenario (scenario:ScenarioSource) =
        let steps = scenario.Steps |> List.ofArray
        let zero = getZero()
        let result = steps |> List.scan perform zero
        // Verbose:
        //    let rec doit output steps =
        //      match steps with
        //      | [] -> output
        //      | step :: more ->
        //          let state = output |> List.head
        //          let newstate = perform state step
        //          let newoutput = newstate :: output
        //          doit newoutput more
        //    let steps = scenario.Steps |> List.ofArray
        //    let zero = getZero
        //    let result = doit [zero] steps
        Assert.Pass(sprintf "Ran %d steps. Final state: %A" (steps |> List.length) result.Head)

        member this.Scenarios = feature.Scenarios |> Seq.filter notIgnored