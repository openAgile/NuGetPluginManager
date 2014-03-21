namespace NuGetPluginManager

module public ListPackages =

    open NUnit.Framework
    open System.Linq
    open System.Collections
    open System
    open NuGetPluginManagerSetup

    let newestVersion = "100.0.0.0"
    let oldestVersion = "20.0.0.0"

    let AssertPackageOrderCorrect (packageGroup : IOrderedEnumerable<OpenAgile.NuGetPluginManager.PackageModel>) =
        // Test BinFoo ordering
        let v2 = Seq.nth 0 packageGroup
        let v1 = Seq.nth 1 packageGroup

        Assert.AreEqual(newestVersion, v2.VersionString)
        Assert.AreEqual(oldestVersion, v1.VersionString)

    [<TestFixture>]
    [<Description("Unit test of the plugin manager when multiple versions of a single package exist in the remote NuGet Repository")>]
    type ``when two versions of the same package are in remote repository``() =
        // Implicit setup?
        let packageId = "MyApp.Awesome.Plugin.BinFoo"

        let availablePackages = makePackages (sprintf "%s|%s, %s|%s" packageId newestVersion packageId oldestVersion)
        let installedPackages = makePackages String.Empty
    
        let subject = makePluginManager installedPackages availablePackages

        let groupedPackages = subject.ListPackages()

        [<Test>]
        member It.``returns one package group``() =
            Assert.AreEqual(1, groupedPackages.Count)

        [<Test>]
        member It.``returns the correct package group``() =
            let packageGroup = groupedPackages.[packageId]
            let package = Seq.nth 0 packageGroup
            Assert.AreEqual(packageId, package.Id)

        [<Test>]
        member It.``returns the distinct package versions with most recent first``() =
            AssertPackageOrderCorrect groupedPackages.[packageId]

    [<TestFixture>]
    type ``when two versions of a different package are in remote repository``() =
        // Implicit setup?
        let packageBinFooId = "MyApp.Awesome.Plugin.BinFoo"
        let packageFubarId = "MyApp.Awesome.Plugin.Fubar"

        let availablePackages = 
            makePackages (sprintf "%s|%s, %s|%s, %s|%s, %s|%s" 
                packageBinFooId newestVersion packageBinFooId oldestVersion 
                packageFubarId newestVersion packageFubarId oldestVersion)
    
        let installedPackages = makePackages String.Empty
    
        let subject = makePluginManager installedPackages availablePackages

        let groupedPackages = subject.ListPackages()

        [<Test>]
        member It.``returns two package groups``() =
            Assert.AreEqual(2, groupedPackages.Count)

        [<Test>]
        member It.``returns the correct package groups``() =
            let packageGroupBinFoo = groupedPackages.[packageBinFooId]
            let packageGroupFubar = groupedPackages.[packageFubarId]
            let packageBinFoo = Seq.nth 0 packageGroupBinFoo
            let packageFubar = Seq.nth 1 packageGroupFubar
            Assert.AreEqual(packageBinFooId, packageBinFoo.Id)
            Assert.AreEqual(packageFubarId, packageFubar.Id)

        [<Test>]
        member It.``returns the distinct package versions with most recent first``() =
            // Test BinFoo ordering
            AssertPackageOrderCorrect groupedPackages.[packageBinFooId]
            // Test Fubar ordering
            AssertPackageOrderCorrect groupedPackages.[packageFubarId]