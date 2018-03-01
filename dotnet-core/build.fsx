let appName = "MyApp.Functions"
open Fake.Core.BuildServer
let sln = sprintf "%s.sln" appName

// ==============================================
// Use this for IDE support. Not required by FAKE 5. Change "build.fsx" to the name of your script.
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.Globbing.Operators
open Fake.DotNet.Cli
open Fake.IO.FileSystemOperators
open Fake.IO.Zip
open Fake.IO.Shell
open Fake.IO.Directory

let version = 
        match buildServer with 
        | TeamFoundation -> buildVersion
        | _ -> "1.0.0-beta4"

let buildDir = __SOURCE_DIRECTORY__ + "/build"
ensure buildDir
let artifactDir = __SOURCE_DIRECTORY__ + "/artifacts"
ensure artifactDir

Target.Create "DotnetInstall" (fun _ ->
    let sdkVersion = "2.1.4"
    let customInstallBaseDirectory = 
        match buildServer with 
        | TeamFoundation -> Environment.environVar "AGENT_BUILDDIRECTORY"
        | LocalBuild -> Environment.environVar "LocalAppData"
        | _ -> __SOURCE_DIRECTORY__ + "/temp"

    let customInstallDirectory = customInstallBaseDirectory @@ "Microsoft" @@ "dotnet" @@ sdkVersion

    let setOptions (options: DotNetCliInstallOptions) =
        { options with 
            InstallerOptions = (fun io ->
                { io with
                    Branch = "release/2.1"
                })
            Channel = None
            Version = Version sdkVersion
            CustomInstallDir = Some customInstallDirectory  }

    DefaultDotnetCliDir <- customInstallDirectory
    DotnetCliInstall setOptions
)

Target.Create "Clean" <| fun _ ->
    CleanDirs [buildDir; artifactDir;]

Target.Create "Build" (fun _ ->
    let buildParams (buildOptions : DotNetBuildOptions) = 
        { buildOptions with OutputPath = Some (buildDir) }
    DotnetBuild buildParams sln
)

Target.Create "Artifact" (fun _ ->
    let artifactFilename = sprintf "%s/%s.%s.zip" artifactDir appName version

    !! "./build/**/*.*"
    |> Zip buildDir artifactFilename

    let artifactDirArm = (artifactDir + "/arm/")
    ensure artifactDirArm
    CopyDir artifactDirArm "infrastructure/arm" (fun _ -> true)

    CopyFile artifactDir "upload.cmd"
    CopyFile artifactDir "upload.ps1"
)

open Fake.Core.TargetOperators

"Clean"
    ==> "DotnetInstall"
    ==> "Build"
    ==> "Artifact"

Target.RunOrDefault "Build"