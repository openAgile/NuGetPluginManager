namespace OpenAgile.NuGetPluginManager

open System
open NuGet
open System.Collections.Generic
open System.Linq
open System.IO

type PackageModel = {
    Id : String
    VersionString : String
    SemanticVersion: NuGet.SemanticVersion
    Description : String
    IsInstalled : bool
    ReleaseNotes : String
    Authors : String
}

type PackageInstalledModel = {
    Id : String
    Version : String
    IsInstalled : bool
}

type NuGetPluginManager(packageFolder, installFolder, packageSearchPattern, fileSystem : NuGet.IFileSystem
    , remotePackageManager : IPackageManager, localPackageManager : IPackageManager) =
    let installMarker packageId packageVersion = installFolder + "\\" + packageId + "." + packageVersion + ".installed"

    let isInstalledInInstallFolder packageId packageVersion =
        fileSystem.FileExists(installFolder + "\\" + packageId + ".dll") && fileSystem.FileExists(installMarker packageId packageVersion)
        //File.Exists(installFolder + "\\" + packageId + ".dll") && File.Exists(installMarker packageId packageVersion)
               
    let copyModuleToInstallFolder packageId packageVersion = 
        let searchPattern = packageId + ".dll"
        let moduleFolder = packageFolder + "\\" + packageId + "." + packageVersion
        for file in fileSystem.GetFiles(moduleFolder, searchPattern, true) do
            let destinationFile = (installFolder + "\\" + file)
            use moduleFileStream = fileSystem.OpenFile(file)
            fileSystem.DeleteFile(destinationFile)
            fileSystem.AddFile(destinationFile, moduleFileStream)
            let fileInstallMarker = installMarker packageId packageVersion
            let installMarkerContents = "installed"B
            use installMarkerStream = new System.IO.MemoryStream(installMarkerContents)           
            fileSystem.AddFile(fileInstallMarker, installMarkerStream)
//        let moduleFolder = DirectoryInfo (packageFolder + "\\" + packageId + "." + packageVersion)
//        for file in moduleFolder.GetFiles(searchPattern, SearchOption.AllDirectories) do
//            let destinationFile = (installFolder + "\\" + file.Name)
//            file.CopyTo(destinationFile, true) |> ignore
//            File.Create(installMarker packageId packageVersion).Dispose()

    let deletePreviousInstallMarkers packageId = 
        let searchPattern = packageId + "*.installed"
        let installFolderFiles = DirectoryInfo(installFolder)
        for file in installFolderFiles.GetFiles(searchPattern) do
            file.Delete()

    let deleteModuleFromInstallFolder packageId packageVersion =        
        let searchPattern = packageId + ".dll"        
        let installFiles = DirectoryInfo installFolder
        for file in installFiles.GetFiles searchPattern do 
            file.Delete()
            File.Delete(installMarker packageId packageVersion)
        ()

    member x.ListPackages () =
        let allPackages = [ 
            for repoPackage in remotePackageManager.SourceRepository.GetPackages() ->
                { 
                    Id = repoPackage.Id;
                    VersionString = repoPackage.Version.ToString();
                    SemanticVersion = repoPackage.Version;
                    Description = repoPackage.Description;
                    IsInstalled = (isInstalledInInstallFolder repoPackage.Id (repoPackage.Version.ToString()) );
                    ReleaseNotes = repoPackage.ReleaseNotes;
                    Authors = String.Join("",repoPackage.Authors.ToArray())
                }
        ]
        let groups = query {
            for p in allPackages do
            groupBy p.Id
        }        
        let groupedByIdSortedByVersion = Dictionary<String, IOrderedEnumerable<PackageModel>>()
        for group in groups do            
            groupedByIdSortedByVersion.Add(group.Key, group.ToList().OrderByDescending(fun g -> g.SemanticVersion))
        groupedByIdSortedByVersion

    member x.ListLatestPackagesByPattern =
        x.ListLatestPackageByPattern packageSearchPattern

    member x.ListLatestPackageByPattern pattern =
        let allPackages = [ 
            for repoPackage in remotePackageManager.SourceRepository.Search(pattern, false) ->
                { 
                    Id = repoPackage.Id;
                    VersionString = repoPackage.Version.ToString();
                    SemanticVersion = repoPackage.Version;
                    Description = repoPackage.Description;
                    IsInstalled = (isInstalledInInstallFolder repoPackage.Id (repoPackage.Version.ToString()) );
                    ReleaseNotes = repoPackage.ReleaseNotes;
                    Authors = String.Join("",repoPackage.Authors.ToArray())
                }
        ]
        let groups = query {
            for p in allPackages do
            groupBy p.Id
        }        
        let groupedByIdOnlyLatestVersion = Dictionary<String, PackageModel>()
        for group in groups do            
            groupedByIdOnlyLatestVersion.Add(group.Key, group.ToList().OrderByDescending(fun g -> g.SemanticVersion).First())
        groupedByIdOnlyLatestVersion

    member x.Install packageId (packageVersion : String) =
        localPackageManager.InstallPackage(packageId, SemanticVersion(packageVersion), false, false)
        deletePreviousInstallMarkers packageId
        copyModuleToInstallFolder packageId packageVersion
        { Id = packageId; Version = packageVersion; IsInstalled = true }

    member x.Uninstall packageId (packageVersion : String) =
        localPackageManager.UninstallPackage(packageId, SemanticVersion(packageVersion), false, false)
        deleteModuleFromInstallFolder packageId packageVersion
        { Id = packageId; Version = packageVersion; IsInstalled = false }

    static member Create rootFolder packageFolder installFolder packageSearchPattern remotePackageManagerRepositoryUrl (additionalPackageRepositoryUrls : IEnumerable<string>) =    
        let mainPackageRepository = NuGet.PackageRepositoryFactory.Default.CreateRepository(remotePackageManagerRepositoryUrl)
        let remotePackageManager = NuGet.PackageManager(mainPackageRepository, packageFolder)

        let additionalPackageRepositories = [
            for packageRepositoryUrl in additionalPackageRepositoryUrls -> NuGet.PackageRepositoryFactory.Default.CreateRepository(packageRepositoryUrl)
        ]
        
        let allRepositories = additionalPackageRepositories.Union([mainPackageRepository])

        let allPackageRepositories = NuGet.AggregateRepository(allRepositories)
        let localPackageManager = NuGet.PackageManager(allPackageRepositories, packageFolder)

        let sourceRepository = remotePackageManager.SourceRepository
        
        let fileSystem = NuGet.PhysicalFileSystem(rootFolder)
        
        NuGetPluginManager(packageFolder, installFolder, packageSearchPattern, fileSystem
            , remotePackageManager, localPackageManager)

    