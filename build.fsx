#r @"tools/FAKE/tools/FakeLib.dll"
#r @"tools/FAKE/tools/Fake.FluentMigrator.dll"
#r @"tools/Nuget.Core/lib/net40-Client/NuGet.Core.dll"

open Fake
open Fake.FluentMigratorHelper
open Fake.Testing.NUnit3

let appName = "MyWebApp.Web"
let connectionStringName = "MyWebAppWeb"

//------------------------------------------------------------------------------
// Variables
//------------------------------------------------------------------------------

let sourceDir = __SOURCE_DIRECTORY__
let buildDir = sourceDir @@ @"\build"
let testOutput = sourceDir @@ @"\testresults"
let artifactDir = sourceDir @@ @"\artifacts"

let runningOnBuildServer =
    match buildServer with
    | LocalBuild -> true // SWITCH TO FALSE BEFORE MERGING
    | _ -> true // | TeamFoundation | TeamCity | AppVeyor etc

let version =
    match buildServer with
    | TeamFoundation | TeamCity -> buildVersion
    | LocalBuild -> "1.0.0-local"
    | _ -> environVarOrDefault "version" "1.0.0"
let buildMode = if runningOnBuildServer then "Release" else "Debug"
let buildOptimize = if runningOnBuildServer then "True" else "False"

let integrationTestDatabaseName = sprintf @"%sIntegrationTests" connectionStringName
let connectionString =  @"Data Source=(LocalDb)\MSSQLLocalDB"
let connectionStringDev = sprintf "%s;Database=%s;Integrated Security=True;Connect Timeout=100;" connectionString integrationTestDatabaseName
let connectionDev = ConnectionString(connectionStringDev,SqlServer(V2014))
let findDllInBuildFolder dllGlob =
    sprintf "%s/%s" (if runningOnBuildServer then buildDir else sprintf "./src/**/bin/%s" buildMode) dllGlob

//------------------------------------------------------------------------------
// Variables
//------------------------------------------------------------------------------

// TestCategories can be used in TestFilters (MSTest) to exclude tests:
// TestCategory!=IgnoreOnVSTSBecauseOfNoAccessToOnPremResource&TestCategory!=NeedsAadCertificateInLocalMachineStore
let setNUnit3Params testResultsFile (defaults : NUnit3Params) =
    let output = sprintf "%s\\%s" testOutput testResultsFile
    { defaults with
        ResultSpecs = [output]
        TeamCity = (buildServer = TeamCity)
        Params = if runningOnBuildServer then "exclude=IgnoreOnVSTSBecauseOfNoAccessToOnPremResource;exclude=NeedsAadCertificateInLocalMachineStore" else ""
        ToolPath = "./tools/Nunit.ConsoleRunner/tools/nunit3-console.exe"  }

let setMsBuildParams (defaults : MSBuildParams) =
    { defaults with
        Verbosity = Some MSBuildVerbosity.Minimal
        ToolsVersion = Some "15.0"
        Properties =
            [
                "Optimize", buildOptimize
                "DebugSymbols", "True"
                "VisualStudioVersion", "15.0"
                "Configuration", getBuildParamOrDefault "buildMode" buildMode
            ] }

MSBuildDefaults <- (setMsBuildParams MSBuildDefaults)

let sln = sprintf "src/%s.sln" appName

//------------------------------------------------------------------------------
// Database creation and deletion
//------------------------------------------------------------------------------

let createDb devDbName =
    let devDbLocation =
        match System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) with
        | home when home <> "" -> home + @"\db\MyWebApp\"
        | _ -> @"C:\db\MyWebApp\"

    let devDbFile = (sprintf "%s%s" devDbLocation devDbName)

    CreateDir devDbLocation

    use connection = new System.Data.SqlClient.SqlConnection(connectionString);

    connection.Open();

    let sql = sprintf "
                IF db_id('%s') IS NULL
                BEGIN
                    CREATE DATABASE
                        [%s]
                    ON PRIMARY (
                       NAME=%s,
                       FILENAME = '%s.mdf'
                    )
                    LOG ON (
                        NAME=%s,
                        FILENAME = '%s.ldf'
                    )
                END" devDbName devDbName devDbName devDbFile (devDbName + "Log") devDbFile

    new System.Data.SqlClient.SqlCommand(sql, connection);
    |> fun c -> c.ExecuteNonQuery()
    |> ignore

let deleteDb devDbName =
    use connection = new System.Data.SqlClient.SqlConnection(connectionString);

    connection.Open();

    let sql = sprintf "
        IF db_id('%s') IS NOT NULL
        BEGIN
            USE master;
            ALTER DATABASE [%s] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [%s]; END" devDbName devDbName devDbName

    new System.Data.SqlClient.SqlCommand(sql, connection);
    |> fun c -> c.ExecuteNonQuery()
    |> ignore

    connection.Dispose();

//------------------------------------------------------------------------------
// Targets
//------------------------------------------------------------------------------

Target "AssemblyInfo" <| fun _ ->
    ReplaceAssemblyInfoVersionsBulk (!! "src/*/Properties/AssemblyInfo.cs") (fun p ->
        {
            p with
                AssemblyVersion = version
                AssemblyCompany = "MyCompany"
                AssemblyCopyright = System.DateTime.Now.ToString("yyyy")
        })

Target "Clean" <| fun _ ->
    CleanDirs [buildDir; artifactDir; testOutput]
    !! sln
    |> MSBuild null "Clean" [] |> ignore

Target "RestorePackages" <| fun _ ->
    sln
    |> RestoreMSSolutionPackages (fun p ->
        { p with
            OutputPath = "./src/packages"
            Retries = 4 })

Target "Build" <| fun _ ->
    !! sln
    |> MSBuild null "Build" [
                                if runningOnBuildServer then yield "OutputPath", buildDir
                            ]
    |> ignore

Target "CreateIntegrationTestsDatabase" <| fun _ ->
    createDb integrationTestDatabaseName

Target "DeleteIntegrationTestsDatabase" <| fun _ ->
    deleteDb integrationTestDatabaseName

Target "MigrateDatabaseCI" <| fun _ ->
    let dll = sprintf "%s.Migrations.dll" appName
    MigrateToLatest connectionDev [findDllInBuildFolder dll] {DefaultMigrationOptions with Profile="CI";}

Target "IntegrationTests" <| fun _ ->
    CreateDir testOutput
    !! (findDllInBuildFolder "*.Tests.Integration.dll")
    |> NUnit3 (setNUnit3Params "IntegrationTestResults.xml")

Target "UnitTests" <| fun _ ->
    CreateDir testOutput
    !! (findDllInBuildFolder "*.Tests.Unit.dll")
    |> NUnit3 (setNUnit3Params "UnitTestResults.xml")

type PackageReferenceFile = NuGet.PackageReferenceFile
Target "NuGetPackagesConsolidated" <| fun _ ->
    !! (sprintf "./src/%s*/packages.config" appName)
    -- "**/obj/**/packages.config"
    |> Seq.map PackageReferenceFile
    |> Seq.collect (fun prf -> prf.GetPackageReferences())
    |> Seq.groupBy (fun pr -> pr.Id)
    |> Seq.filter (fun p -> (snd p |> Seq.distinct |> Seq.length) > 1 )
    |> Seq.map (fun p -> fst p , snd p |> Seq.distinct)
    |> function 
        | packages when packages |> Seq.isEmpty -> ()
        | packages -> 
            seq {
                yield "The following packages are not consolidated:"

                for (k,v) in packages do
                    yield (sprintf "    Package: %s Versions: %A" k v)
            
                yield "You need to consolidate packages across the solution:"
                yield "    * Right click on the solution inside VS"
                yield "    * Choose Manage NuGet Packages for Solution"
                yield "    * Choose the Consolidate tab"
                yield "    * Make sure you sync the package versions" }
            |> Seq.iter (printfn "%s")
            failwith "Packages not consolidated"

Target "Artifact" <| fun _ ->
    let preZip = sprintf "%s/prezip" buildDir
    let preZipTools = (preZip + "/tools/")
    let artifactFilename = sprintf "%s/%s.%s.zip" artifactDir appName version

    CopyDir preZip (sprintf "%s/_PublishedWebsites" buildDir) (fun _ -> true)
    CopyDir preZip (sprintf "%s/_PublishedApplications" buildDir) (fun _ -> true)
    CopyFiles preZip [".deployment"; "deploy.cmd"; "deploy.fsx"]
    
    CreateDir preZipTools
    CopyFile preZipTools "tools/nuget.exe"

    !! "./build/prezip/**/*.*"
    |> Zip preZip artifactFilename

    CopyFile artifactDir "upload.cmd"
    CopyFile artifactDir "upload.ps1"

Target "CI" <| DoNothing

//------------------------------------------------------------------------------
// Dependencies
//------------------------------------------------------------------------------

"Clean"
    ==> "RestorePackages"
    ==> "Build"

"Build"
    ==> "DeleteIntegrationTestsDatabase"
    ==> "CreateIntegrationTestsDatabase"
    ==> "MigrateDatabaseCI"
    ==> "IntegrationTests"

"NuGetPackagesConsolidated" ==> "CI"
"IntegrationTests" ==> "CI"
"UnitTests"  ==> "CI"

"Build" ==> "UnitTests"

"CI" ==> "Artifact"

RunTargetOrDefault "CI"