// Learn more about F# at http://fsharp.org

open System
open System.Reflection
open Microsoft.Data.Mashup
open Microsoft.Data.Mashup.Preview
open System.IO

[<EntryPoint>]
let main argv =
    do MashupLibraryProvider.SetProviders (MashupLibraryProvider.Assembly (AssemblyName "PowerBIExtensions"))
    let code = File.ReadAllText argv.[0]
    do Console.WriteLine "Select Query:"
    let queryName = Console.ReadLine ()
    let queryParameters = MHelper.GetParameterQueries code
    let userInputForParameters = fun queryParameter ->
        do Console.Write()
    let parameterReplacements = List.map (fun queryParameter -> )


    0 // return an integer exit code
