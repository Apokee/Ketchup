#r "packages/FAKE/tools/FakeLib.dll"
open Fake

// Properties
let kspDir = "C:/Program Files (x86)/Steam/SteamApps/common/Kerbal Space Program"
let kspDepDir = kspDir + "/KSP_Data/Managed"
let buildDir = "./Output"

// Targets
Target "Default" (fun _ ->
    trace "Hello World"
)

Target "Init" (fun _ ->
    CreateDir "./Dependencies/KSP"
    Copy "./Dependencies/KSP" [kspDepDir + "/Assembly-CSharp.dll"; kspDepDir + "/UnityEngine.dll"]
)

Target "Clean" (fun _ ->
    CleanDir buildDir
)

"Init"
    ==> "Default"
"Clean"
    ==> "Default"

RunTargetOrDefault "Default"
