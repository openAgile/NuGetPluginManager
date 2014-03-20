module NuGetPluginManagerSetup

open OpenAgile.NuGetPluginManager
open System
open System.Linq
open NuGet
open Foq

let trim (str : string) = str.Trim()

let makePackages (plugins : string) =  
    if String.IsNullOrWhiteSpace(plugins) then 
        [] |> Queryable.AsQueryable
    else
          
        [ for pluginDef in plugins.Split(',') ->
            let parts = pluginDef.Split('|')
            let name = trim parts.[0]
            let version = trim parts.[1]

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

let makePluginManager (installedPackages : IQueryable<IPackage>) (availablePackages : IQueryable<IPackage>) =    
    let searchPackages = (fun (pattern, allowPrelease) -> query {
                for package in installedPackages do
                where (package.Id.StartsWith pattern)
                select package
            })
    
    let findInstalledPackage (fileName : string) =
        Seq.exists (fun (x : IPackage) -> fileName.Contains(x.Id)) installedPackages
    
    let fileSystem = 
        Mock<IFileSystem>()
            .Setup(fun x -> <@ x.FileExists(any()) @>).Calls<string>(findInstalledPackage)
            .Create()

    let fakeRepository = 
        Mock<NuGet.IPackageRepository>()
            .Setup(fun x -> <@ x.GetPackages() @>).Returns(availablePackages)
            //SW.Setup(fun x -> <@ x.Search(any(), any()) @>).Calls<string * bool>(searchPackages)
            .Create()

    let remotePackagegManager = 
        Mock<NuGet.IPackageManager>()
            .Setup(fun x -> <@ x.SourceRepository @>).Returns(fakeRepository)
            .Create()
    
    let localPackageManager = Mock<NuGet.IPackageManager>().Create()
    
    NuGetPluginManager("", "", "MyApp.Awesome.Plugin."
        ,fileSystem, remotePackagegManager, localPackageManager)
