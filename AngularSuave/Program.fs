
open Suave
open Suave.Http
open Suave.Http.Successful
open Suave.Http.Redirection
open Suave.Http.Applicatives
open Suave.Http.Files
open Suave.Web
open Suave.Http.Writers
open System.Threading
open System
open System.IO

//add json to mimeTypes so browseFile returns it with correct mime type
let mimeTypes =
    defaultMimeTypesMap
        >=> (function | ".json" -> mkMimeType "application/json" true | _ -> None)

//define root path where the web stuff is located
let rootPath = Path.GetFullPath "../../../Web"

//customize default server config with own mimetypes & homeFolder
let webConfig = 
    { 
        defaultConfig with 
            homeFolder = Some rootPath
            mimeTypesMap = mimeTypes
    }

//"api" call for getting the phone data
let getPhone phoneName =
    printfn "getting data for phone: %s" phoneName

    let phoneFolder = Path.Combine(rootPath, "app", "phones")
    browseFile phoneFolder phoneName

//simple return requested file
let getFile name =
    let rootPath = webConfig.homeFolder.Value
    browseFile rootPath name

//api webpart 
let api =
    choose
        [
            GET >>= choose 
                [
                    pathScan "/api/phones/%s" (fun s -> getPhone s)
                ]
        ]

//angular app webpart with default redirect
let angularApp =
    choose
        [ GET >>= choose
            [
                path "/" >>=  redirect "app/index.html"
                pathScan "/%s" (fun s -> getFile s)                
            ]
        ]

//general app webpart to combine api & angular webpart
let app =
    choose
        [
            api
            angularApp
        ]

[<EntryPoint>]
let main argv = 
    
    let cts = new CancellationTokenSource()
    let startingServer, shutdownServer = startWebServerAsync webConfig app

    Async.Start(shutdownServer, cts.Token)

    startingServer |> Async.RunSynchronously |> printfn "started: %A"

    printfn "Press Enter to stop"
    Console.Read() |> ignore

    cts.Cancel()

    0
