#r "packages/FAKE/tools/FakeLib.dll"
open Fake

// Properties
let kspDir = "C:/Program Files (x86)/Steam/SteamApps/common/Kerbal Space Program" // TODO: make configurable
let kspDepDir = kspDir + "/KSP_Data/Managed"
let outputDir = "./Output"
let buildDir = outputDir + "/Build"

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
        |> Log "BuildMod-Output: "
)

Target "Clean" (fun _ ->
    CleanDir outputDir
)

// Dependencies
"Init"
    ==> "BuildMod"
"Clean"
    ==> "BuildMod"

"BuildMod"
    ==> "Default"

// Start
RunTargetOrDefault "Default"
