#I @"src/packages/FAKE/tools"
#r "FakeLib.dll"

open Fake
open System
open System.IO
open Fake.FileUtils

//--------------------------------------------------------------------------------
// Information about the project for Nuget and Assembly info files
//--------------------------------------------------------------------------------

let authors = [ "Roman Tumaykin" ]
let copyright = "Copyright © 2017 Roman Tumaykin"
let company = "Roman Tumaykin"
let description = "A command line utility that builds an ispac file from a Visual Studio SSIS project (project deployment model only)"
let tags = ["SSIS";"build";"tools"]
let configuration = "Release"


// Read release notes and version

let parsedRelease =
    File.ReadLines "RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

let envBuildNumber = System.Environment.GetEnvironmentVariable("BUILD_NUMBER")
let buildNumber = if String.IsNullOrWhiteSpace(envBuildNumber) then "0" else envBuildNumber

let version = parsedRelease.AssemblyVersion + "." + buildNumber
let preReleaseVersion = version + "-beta"

let isPreRelease = hasBuildParam "nugetprerelease"
let release = if isPreRelease then ReleaseNotesHelper.ReleaseNotes.New(version, version + "-beta", parsedRelease.Notes) else parsedRelease

printfn "Assembly version: %s\nNuget version; %s\n" release.AssemblyVersion release.NugetVersion
//--------------------------------------------------------------------------------
// Directories

let buildDir = "./build/"
let testDir = FullName "TestResults"

open Fake.RestorePackageHelper
Target "RestorePackages" (fun _ -> 
     "./src/ssis-build.sln"
     |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = "./src/packages"
             Retries = 4 })
 )

//--------------------------------------------------------------------------------
// Clean build results

Target "Clean" <| fun _ ->
    DeleteDir buildDir
    DeleteDir testDir


//--------------------------------------------------------------------------------
// Build the solution

Target "Build" <| fun _ ->

    !!"src/ssis-build.sln"
    |> MSBuildRelease "" "Clean,Rebuild"
    |> ignore

//--------------------------------------------------------------------------------
// Run tests

open Fake.Testing
Target "RunTests" <| fun _ ->  
    let xunitTestAssemblies = !! "src/**/bin/Release/*.Tests.dll"

    mkdir testDir
   
    let xunitToolPath = findToolInSubPath "xunit.console.exe" "src/packages/FAKE/xunit.runner.console*/tools"

    printfn "Using XUnit runner: %s" xunitToolPath
    let runSingleAssembly assembly =
        let assemblyName = Path.GetFileNameWithoutExtension(assembly)
        xUnit2
            (fun p -> { p with XmlOutputPath = Some (testDir + @"\" + assemblyName + "_xunit.xml"); HtmlOutputPath = Some (testDir + @"\" + assemblyName + "_xunit.HTML"); ToolPath = xunitToolPath; TimeOut = System.TimeSpan.FromMinutes 30.0; Parallel = ParallelMode.NoParallelization }) 
            (Seq.singleton assembly)

    xunitTestAssemblies |> Seq.iter (runSingleAssembly)
    
//--------------------------------------------------------------------------------
// Generate AssemblyInfo files with the version for release notes 

open AssemblyInfoFile

Target "AssemblyInfo" <| fun _ ->
    CreateCSharpAssemblyInfoWithConfig "src/SharedAssemblyInfo.cs" [
        Attribute.Company company
        Attribute.Copyright copyright
        Attribute.Trademark ""
        Attribute.Version version
        Attribute.FileVersion version ] <| AssemblyInfoFileConfig(false)

Target "CreatePackage" (fun _ ->
    let project = "SSISBuild"
    let title = "SSIS Build"


    mkdir buildDir

    let nuspecFile = @"src\SsisBuild\SsisBuild.nuspec"

    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/id" project
    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/version" release.NugetVersion
    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/title" title
    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/authors" (authors |> String.concat " ")
    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/owners" (authors |> String.concat " ")
    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/description" description
    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/releaseNotes" (release.Notes |> String.concat "\n")
    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/tags" (tags |> String.concat " ")
    XMLHelper.XmlPokeInnerText nuspecFile @"/package/metadata/copyright" copyright

    NuGetPack (fun p -> 
        {p with
            Authors = authors
            Copyright = copyright
            Description = description                               
            IncludeReferencedProjects = true
            OutputPath = buildDir
            Project = project
            Properties = ["Configuration", "Release"]
            ReleaseNotes = release.Notes |> String.concat "\n"
            Summary = description
            Version = release.NugetVersion
            Tags = tags |> String.concat " "
            Title = title
            SymbolPackage = NugetSymbolPackage.ProjectFile
            WorkingDir = @"src\SsisBuild"
            Publish = false }) 
            @"src\SsisBuild\SsisBuild.nuspec"
)

Target "BuildRelease" DoNothing


//--------------------------------------------------------------------------------
// Help 
//--------------------------------------------------------------------------------

Target "Help" <| fun _ ->
    List.iter printfn [
      "usage:"
      "build [target]"
      ""
      " Targets for building:"
      " * Build      Builds"
      " * Nuget      Create and optionally publish nugets packages"
      " * RunTests   Runs tests"
      " * All        Builds, run tests, creates and optionally publish nuget packages"
      ""
      " Other Targets"
      " * Help       Display this help" 
      " * HelpNuget  Display help about creating and pushing nuget packages" 
      ""]

Target "HelpNuget" <| fun _ ->
    List.iter printfn [
      "usage: "
      "build Nuget [nugetkey=<key> [nugetpublishurl=<url>]] "
      "            [symbolskey=<key> symbolspublishurl=<url>] "
      "            [nugetprerelease=<prefix>]"
      ""
      "Arguments for Nuget target:"
      "   nugetprerelease=<prefix>   Creates a pre-release package."
      "                              The version will be version-prefix<date>"
      "                              Example: nugetprerelease=dev =>"
      "                                       0.6.3-dev1408191917"
      ""
      "In order to publish a nuget package, keys must be specified."
      "If a key is not specified the nuget packages will only be created on disk"
      "After a build you can find them in bin/nuget"
      ""
      "For pushing nuget packages to nuget.org and symbols to symbolsource.org"
      "you need to specify nugetkey=<key>"
      "   build Nuget nugetKey=<key for nuget.org>"
      ""
      "For pushing the ordinary nuget packages to another place than nuget.org specify the url"
      "  nugetkey=<key>  nugetpublishurl=<url>  "
      ""
      "For pushing symbols packages specify:"
      "  symbolskey=<key>  symbolspublishurl=<url> "
      ""
      "Examples:"
      "  build Nuget                      Build nuget packages to the bin/nuget folder"
      ""
      "  build Nuget nugetprerelease=dev  Build pre-release nuget packages"
      ""
      "  build Nuget nugetkey=123         Build and publish to nuget.org and symbolsource.org"
      ""
      "  build Nuget nugetprerelease=dev nugetkey=123 nugetpublishurl=http://abc"
      "              symbolskey=456 symbolspublishurl=http://xyz"
      "                                   Build and publish pre-release nuget packages to http://abc"
      "                                   and symbols packages to http://xyz"
      ""]


//--------------------------------------------------------------------------------
//  Target dependencies
//--------------------------------------------------------------------------------

// build dependencies
"Clean" ==> "RestorePackages" ==> "AssemblyInfo" ==> "Build" ==> "RunTests" ==> "CreatePackage" ==> "BuildRelease"




RunTargetOrDefault "Help"
