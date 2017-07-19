#r @"packages/FAKE/tools/FakeLib.dll"
open System
open Fake
open Fake.AssemblyInfoFile

let buildDir = System.IO.Path.Combine(Environment.CurrentDirectory, "build")
let assemblyInfoFile = System.IO.Path.Combine(Environment.CurrentDirectory, "Properties", "AssemblyInfo.cs")
let projectFileName = !! "*.csproj" |> Seq.head
let projectName =  fileNameWithoutExt projectFileName

let getVersion file =
    match GetAttributeValue "AssemblyVersion" file with
    | Some v -> v
    | None -> "0.0.0.0"

let build env =
    CleanDir buildDir
    let version = getVersion assemblyInfoFile
    let mode = env
    let buildMode = getBuildParamOrDefault "buildMode" mode
    let setParams defaults =
        { defaults with
            Verbosity = Some(Quiet)
            Targets = ["Build"]
            Properties =
                [
                    "OutputPath", buildDir
                    "Optimize", "True"
                    "DebugSymbols", "False"
                    "Configuration", buildMode
                ]
        }
    
    build setParams projectFileName

    Paket.Pack (fun p -> 
        { p with
            TemplateFile = mode + ".paket.template"
            Version = version
            OutputPath = buildDir
    })
    
Target "Release" (fun _ ->
    build "Release"
)

RunTargetOrDefault "Release"