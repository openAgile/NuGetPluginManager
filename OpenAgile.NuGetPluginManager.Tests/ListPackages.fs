namespace NuGetPluginManager

module public ListPackages =
    open NUnit.Framework
    open System.Linq
    open System.Collections
    open System
    open NuGetPluginManagerSetup
    open FsUnit

    let newestVersion = "100.0.0.0"
    let oldestVersion = "20.0.0.0"

    let should_be_ordered_descending_by_SemanticVersion (packageGroup : IOrderedEnumerable<OpenAgile.NuGetPluginManager.PackageModel>) =
        Assert.That(packageGroup, Is.Ordered.Descending.By("SemanticVersion"))

    [<TestFixture>]
    [<Description("Unit test of the plugin manager when multiple versions of a single package exist in the remote NuGet Repository")>]
    type ``when two versions of the same package are in remote repository``() =
        // Implicit setup?
        let packageId = "MyApp.Awesome.Plugin.BinFoo"

        let availablePackages = sprintf "%s|%s, %s|%s" packageId newestVersion packageId oldestVersion |> makePackages
        let installedPackages = String.Empty |> makePackages
    
        let subject = makePluginManager installedPackages availablePackages

        let groupedPackages = subject.ListPackages()

        [<Test>]
        member It.``returns one package group``() = groupedPackages.Count |> should equal 1

        [<Test>]
        member It.``returns the correct package group``() =
            let packageGroup = groupedPackages.[packageId]
            let package = Seq.nth 0 packageGroup
            package.Id |> should equal packageId

        [<Test>]
        member It.``returns packages ordered descending by SemanticVersion``() =
            groupedPackages.[packageId] |> should_be_ordered_descending_by_SemanticVersion

    [<TestFixture>]
    type ``when two versions of a different package are in remote repository``() =
        // Implicit setup?
        let packageBinFooId = "MyApp.Awesome.Plugin.BinFoo"
        let packageFubarId = "MyApp.Awesome.Plugin.Fubar"

        let availablePackages = 
            sprintf "%s|%s, %s|%s, %s|%s, %s|%s" 
                packageBinFooId newestVersion packageBinFooId oldestVersion 
                packageFubarId newestVersion packageFubarId oldestVersion |> makePackages
    
        let installedPackages = String.Empty |> makePackages
    
        let subject = makePluginManager installedPackages availablePackages

        let groupedPackages = subject.ListPackages()

        [<Test>]
        member It.``returns two package groups``() = groupedPackages |> should equal 2

        [<Test>]
        member It.``returns the correct package groups``() =
            let packageGroupBinFoo = groupedPackages.[packageBinFooId]
            let packageGroupFubar = groupedPackages.[packageFubarId]
            let packageBinFoo = Seq.nth 0 packageGroupBinFoo
            let packageFubar = Seq.nth 1 packageGroupFubar
            packageBinFoo.Id |> should equal packageBinFooId
            packageFubar.Id |> should equal packageFubarId


        [<Test>]
        member It.``returns packages ordered descending by SemanticVersion``() =
            groupedPackages.[packageBinFooId] |> should_be_ordered_descending_by_SemanticVersion
            groupedPackages.[packageFubarId] |> should_be_ordered_descending_by_SemanticVersion