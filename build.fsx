#r "packages/FAKE/tools/FakeLib.dll"
open Fake

// Properties
let kspDir = "C:/Program Files (x86)/Steam/SteamApps/common/Kerbal Space Program" // TODO: make configurable
let kspDepDir = kspDir + "/KSP_Data/Managed"
let kspDeployDir = kspDir + "/GameData/Ketchup"

let contribDir = "./Contrib"
let partsDir = "./Parts"

let outputDir = "./Output"
let buildDir = outputDir + "/Build"
let testDir = outputDir + "/Test"
let stageDir = outputDir + "/Stage/Ketchup"

let buildMode = getBuildParamOrDefault "BuildMode" "Debug"

// TODO: figure out a more elegant solution
//if buildMode <> "Debug" && buildMode <> "Release"
//    then raise (System.ArgumentException("Unknown BuildMode " + buildMode))    

MSBuildDefaults <- MSBuildDefaults |> (fun p ->
    { p with
        Verbosity = Some(Minimal)
        Properties = []
    }
)

RestorePackages()

// Targets
Target "Default" (fun _ ->
    trace "Hello World"
)

Target "Init" (fun _ ->
    CreateDir "./Dependencies/KSP"
    Copy "./Dependencies/KSP" [kspDepDir + "/Assembly-CSharp.dll"; kspDepDir + "/UnityEngine.dll"]
)

Target "BuildMod" (fun _ ->
    !! "Source/**/*.csproj"
        -- "**/*.Tests.csproj"
        |> MSBuild (buildDir + "/" + buildMode) "Build" ["Configuration", buildMode]
        |> ignore
)

Target "BuildTest" (fun _ ->
    !! "Source/**/*.Tests.csproj"
        |> MSBuildDebug testDir "Build"
        |> ignore
)

Target "Test" (fun _ ->
    !! (testDir + "/*.Tests.dll")
        |> xUnit (fun p ->
            p
        )
)

Target "Stage" (fun _ ->
    CopyDir (stageDir + "/Contrib") contribDir (fun f -> true)
    CopyDir (stageDir + "/Parts") partsDir (fun f -> true)
    CopyDir (stageDir + "/Plugins") (buildDir + "/" + buildMode) (fun f -> true)
)

Target "Deploy" (fun _ ->
    CleanDir kspDeployDir
    CopyDir kspDeployDir stageDir (fun f -> true)
)

Target "Run" (fun _ ->
    // TODO: this should be asynchronous but FAKE kills the process if you use StartProcess
    ExecProcess (fun psi ->
        psi.FileName <- kspDir + "/KSP.exe"
        psi.WorkingDirectory <- kspDir
    ) (System.TimeSpan.FromMinutes 60.0) |> ignore
)

Target "Clean" (fun _ ->
    CleanDir outputDir
)

// Dependencies
"Init"
    ==> "Clean"
    ==> "BuildMod"
    ==> "BuildTest"
    ==> "Test"
    ==> "Stage"
    ==> "Default"
    ==> "Deploy"
    ==> "Run"

// Start
RunTargetOrDefault "Default"
