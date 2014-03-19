module ManagePluginsSteps

open TickSpec
open NUnit.Framework
open OpenAgile.TickedOffTest
open OpenAgile.NuGetPluginManager
open System.Linq
open System.Collections.Generic
open System.Collections
open System
open Foq

let makePackages (plugins : string) =  
    if String.IsNullOrWhiteSpace(plugins) then 
      [] |> Queryable.AsQueryable
    else      
        [ for pluginDef in plugins.Split(',') ->
            let parts = pluginDef.Split('|')
            let name = parts.[0]
            let version = parts.[1]

            let pkg = 
                Mock<NuGet.IPackage>()
                    .Setup(fun x -> <@ x.Id @>).Returns(name)
                    .Setup(fun x -> <@ x.Version @>).Returns(NuGet.SemanticVersion(version))
                    .Create()
            pkg
        ] |> Queryable.AsQueryable
        
type State = { 
    AvailablePackages : IQueryable<NuGet.IPackage>
    InstalledPackages : IQueryable<NuGet.IPackage>
    //PluginManager : NuGetPluginManager
  }
    with static member Create () = { 
            AvailablePackages = makePackages String.Empty
            InstalledPackages = makePackages String.Empty
            //PluginManager = null
        }

let performStep (state:State) (step, line:LineSource) =
    match step with
    | Given "a NuGet repository with plugins (.*)" [pluginList] ->
        { state with AvailablePackages = makePackages pluginList }
    | Given "no plugins currently installed" [] ->
        { state with InstalledPackages = makePackages String.Empty }
//    | When "requesting the list of available plugins" [] ->
//        { state with }
  

    | _ -> sprintf "Unmatched line %d" line.Number |> invalidOp

[<TestFixture>]
type ManagePluginsFixture() =
    inherit OpenAgile.TickedOffTest.FeatureFixture<State>("ManagePlugins.feature"
    , State.Create, performStep, System.Reflection.Assembly.GetExecutingAssembly())