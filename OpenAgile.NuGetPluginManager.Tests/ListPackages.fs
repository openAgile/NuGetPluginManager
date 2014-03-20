module public ListPackages

open NUnit.Framework
open System.Linq
open System.Collections
open System
open NuGetPluginManagerSetup

[<TestFixture>]
type ListPackages_when_two_versions_of_the_same_package_are_in_remote_repository() =
    // Implicit setup?
    let packageId = "MyApp.Awesome.Plugin.BinFoo"
    let newestVersion = "100.0.0.0"
    let oldestVersion = "20.0.0.0"

    let availablePackages = makePackages (sprintf "%s|%s, %s|%s" packageId newestVersion packageId oldestVersion)
    let installedPackages = makePackages String.Empty
    
    let subject = makePluginManager installedPackages availablePackages

    let groupedPackages = subject.ListPackages()

    [<Test>]
    member t.found_one_grouping() =
        Assert.AreEqual(1, groupedPackages.Count)

    [<Test>]
    member t.found_correct_package() =
        let packageGroup = groupedPackages.[packageId]
        let package = Seq.nth 0 packageGroup
        Assert.AreEqual(packageId, package.Id)

    [<Test>]
    member t.packages_sorted_with_most_recent_at_top() =
        let packageGroup = groupedPackages.[packageId]
        let v2 = Seq.nth 0 packageGroup
        let v1 = Seq.nth 1 packageGroup
        Assert.AreEqual(newestVersion, v2.VersionString)
        Assert.AreEqual(oldestVersion, v1.VersionString)