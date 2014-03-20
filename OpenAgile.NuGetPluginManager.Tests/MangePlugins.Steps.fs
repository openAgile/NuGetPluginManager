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

            let description = "Standard module desc"
            let releaseNote = "Yeah, release notes"

            let pkg = 
                Mock<NuGet.IPackage>()
                    .Setup(fun x -> <@ x.Id @>).Returns(name)
                    .Setup(fun x -> <@ x.Version @>).Returns(NuGet.SemanticVersion(version))
                    .Setup(fun x -> <@ x.Description @>).Returns(description)
                    .Setup(fun x -> <@ x.ReleaseNotes @>).Returns(releaseNote)
                    .Setup(fun x -> <@ x.Authors @>).Returns([])
                    .Create()
            pkg
        ] |> Queryable.AsQueryable

let makePluginManager installedPackages availablePackages =
    let remotePackagegManager = Mock<NuGet.IPackageManager>().Create()
    let localPackageManager = Mock<NuGet.IPackageManager>().Create()
    NuGetPluginManager("", "", "MyApp.Awesome.Plugin."
        , (fun () -> availablePackages )
        , (fun (pattern, allowPrelease) -> query {
                for package in availablePackages do
                where (package.Id.StartsWith pattern)
                select package
            })
        , remotePackagegManager, localPackageManager)
        
type State = { 
    AvailablePackages : IQueryable<NuGet.IPackage>
    InstalledPackages : IQueryable<NuGet.IPackage>
    PluginManager : NuGetPluginManager
    FoundPlugins : IDictionary<String, IOrderedEnumerable<PackageModel>>
  }
    with static member Create () = 
            let availablePackages = makePackages String.Empty
            let installedPackages = makePackages String.Empty
            let state = { 
                AvailablePackages = availablePackages
                InstalledPackages = installedPackages
                PluginManager = makePluginManager installedPackages availablePackages
                FoundPlugins = null
            }
            state

let performStep (state:State) (step, line:LineSource) =
    match step with
    | Given "a NuGet repository with plugins (.*)" [pluginList] ->
        { state with AvailablePackages = makePackages pluginList }
    | Given "no plugins currently installed" [] ->
        { state with InstalledPackages = makePackages String.Empty }
    | When "requesting the list of available plugins" [] ->
        let pluginManager = makePluginManager state.InstalledPackages state.AvailablePackages
        let foundPlugins = pluginManager.ListPackages()
        { state with PluginManager = pluginManager; FoundPlugins = foundPlugins }

    | Then "I should see (.*)" [pluginList] ->
        let expectedPlugins = makePackages pluginList
        // TODO better way:
        let pairs = List.zip (List.ofSeq expectedPlugins) (List.ofSeq state.AvailablePackages)
        for item in pairs do
            let expected, actual = item
            NUnit.Framework.Assert.AreEqual(expected.Id, actual.Id)
            NUnit.Framework.Assert.AreEqual(expected.Version, actual.Version)
        state
    | _ -> sprintf "Unmatched line %d" line.Number |> invalidOp

[<TestFixture>]
type ManagePluginsFixture() =
    inherit OpenAgile.TickedOffTest.FeatureFixture<State>("ManagePlugins.feature"
    , State.Create, performStep, System.Reflection.Assembly.GetExecutingAssembly())