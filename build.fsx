#r "Dependencies/NuGet/FAKE.2.18.2/tools/FakeLib.dll"
#r "Dependencies/NuGet/YamlDotNet.3.2.0/lib/net35/YamlDotNet.dll"
open System.IO
open System.Collections.Generic
open Fake
open YamlDotNet.RepresentationModel

// Properties
let config = (
    // TODO: Clean this up
    // * Cleaner checking of which file to use
    // * Deserialize the file to an object
    let mutable configFile = null

    if File.Exists("build.yml") then
        configFile <- "build.yml"
    else if File.Exists("../Ketchup.build.yml") then
        configFile <- "../Ketchup.build.yml"

    if configFile = null then
        new YamlStream()
    else (
        let yaml = new YamlStream()
        yaml.Load(new StringReader(ReadFileAsString configFile))
        yaml
    )
)

let kspDir = lazy (
    if config.Documents.Count = 0 then
        raise (System.Exception("ksp_dir not specified in configuration"))
    else (
        let mapping = config.Documents.[0].RootNode :?> YamlMappingNode
        let node = new YamlScalarNode("ksp_dir")
        if mapping.Children.ContainsKey(node) then
            mapping.Children.[node].ToString()
        else
            raise (System.Exception("ksp_dir not specified in configuration"))
    )
)

let kspExe = lazy (
    if config.Documents.Count = 0 then
        "KSP.exe"
    else (
        let mapping = config.Documents.[0].RootNode :?> YamlMappingNode
        let node = new YamlScalarNode("ksp_exe")
        if mapping.Children.ContainsKey(node) then
            mapping.Children.[node].ToString()
        else
            "KSP.exe"
    )
)

let kspProfile = lazy (
    if config.Documents.Count = 0 then
        raise (System.Exception("profile not specified in configuration"))
    else (
        let mapping = config.Documents.[0].RootNode :?> YamlMappingNode
        let node = new YamlScalarNode("profile")
        if mapping.Children.ContainsKey(node) then
            mapping.Children.[node].ToString()
        else
            raise (System.Exception("profile not specified in configuration"))
    )
)

let kspDeployName = "Ketchup"
let kspDepDir = lazy (kspDir.Force() + "/KSP_Data/Managed")
let kspDeployDir = lazy (kspDir.Force() + "/GameData/" + kspDeployName)
let kspFirmwareDir = lazy (kspDir.Force() + "/saves/" + kspProfile.Force() + "/Ketchup/Firmware")
let kspLocalDepDir = "./Dependencies/KSP"
let kspAssemblies = ["Assembly-CSharp.dll"; "Assembly-CSharp-firstpass.dll"; "UnityEngine.dll"]

let contribDir = "./Contrib"
let partsDir = "./Parts"
let patchesDir = "./Patches"

let outputDir = "./Output"
let buildDir = outputDir + "/Build"
let testDir = outputDir + "/Test"
let stageDir = outputDir + "/Stage/GameData"
let stageModDir = outputDir + "/Stage/GameData/" + kspDeployName
let packageDir = outputDir + "/Package"
let contribBuildDir = outputDir + "/Contrib"

let buildConfig = getBuildParamOrDefault "Configuration" "Debug"

MSBuildDefaults <- MSBuildDefaults |> (fun p ->
    { p with
        Verbosity = Some(Minimal)
        Properties = []
    }
)

// Targets
Target "Default" (fun _ ->
    trace "Default Target"
)

Target "Init" (fun _ ->
    if not (TestDir kspLocalDepDir) then (
        CreateDir kspLocalDepDir
    )

    kspAssemblies |> List.choose (fun a ->
        match a with
        | a when not (System.IO.File.Exists (kspLocalDepDir + "/" + a)) -> Some(kspDepDir.Force() + "/" + a)
        | _ -> None
    ) |> Copy kspLocalDepDir
)

Target "BuildMod" (fun _ ->
    !! "Source/**/*.csproj"
        |> MSBuild (buildDir + "/" + buildConfig) "Build" ["Configuration", buildConfig]
        |> ignore
)

Target "BuildTest" (fun _ ->
    !! "Tests/**/*.csproj"
        |> MSBuildDebug testDir "Build"
        |> ignore
)

Target "BuildContrib" (fun _ ->
    !! "Contrib/**/*.dasm"
        |> Seq.iter (fun f -> 
            ExecProcess (fun psi ->
                psi.FileName <- "Dependencies/Organic/Organic.exe"
                psi.Arguments <- f + " " + contribBuildDir + "/" + (new FileInfo(f)).Name.Replace(".dasm", ".bin")
            ) (System.TimeSpan.FromSeconds 30.0) |> ignore
        )
)

Target "Test" (fun _ ->
    !! (testDir + "/*.Tests.dll")
        |> xUnit (fun p ->
            p
        )
)

Target "Stage" (fun _ ->
    CopyDir (stageModDir + "/Contrib") contribDir (fun f -> true)
    CopyDir (stageModDir + "/Parts") partsDir (fun f -> true)
    CopyDir (stageModDir + "/Patches") patchesDir (fun f -> true)
    CopyDir (stageModDir + "/Plugins") (buildDir + "/" + buildConfig) (fun f -> true)
)

Target "Deploy" (fun _ ->
    CleanDir (kspDeployDir.Force())
    CleanDir (kspFirmwareDir.Force())

    CopyDir (kspDeployDir.Force()) stageModDir (fun f -> true)
    CopyDir (kspFirmwareDir.Force()) contribBuildDir (fun f -> true)
)

Target "Run" (fun _ ->
    // TODO: Needs to be executed with Mono on Unix-like systems
    // TODO: this should be asynchronous but FAKE kills the process if you use StartProcess
    ExecProcess (fun psi ->
        psi.FileName <- kspDir.Force() + "/" + kspExe.Force()
        psi.WorkingDirectory <- kspDir.Force()
    ) (System.TimeSpan.FromMinutes 60.0) |> ignore
)

Target "Package" (fun _ ->
    // TODO: assert that this has a value
    let version = getBuildParam "Version"

    CreateDir packageDir
    !! (stageDir + "/**/*.*")
        |> Zip (stageDir + "/..") (packageDir + "/Ketchup-" + version + ".zip")
)

Target "Clean" (fun _ ->
    CleanDir outputDir
)

// Dependencies
"Init"
    ==> "Clean"
    ==> "BuildMod"
    ==> "BuildTest"
    ==> "Default"

"Default"
    ==> "Test"
    ==> "Stage"

"Stage"
    ==> "BuildContrib"
    ==> "Deploy"
    ==> "Run"

"Stage"
    ==> "Package"

// Start
RunTargetOrDefault "Default"
