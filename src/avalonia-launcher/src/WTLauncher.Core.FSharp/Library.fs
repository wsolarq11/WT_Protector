namespace WTLauncher.Core.FSharp

open System
open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Serialization

// Data records (immutable by default in F#)
type LaunchEntry = {
    Id: string
    Name: string
    Directory: string
    Command: string
    Selected: bool
    UpdatedAtUtc: string
    LaunchedAtUtc: string
    LaunchCount: int
}

type LauncherWindowSettings = {
    Width: double
    Height: double
    X: double
    Y: double
    WindowState: string
}

type LauncherSettings = {
    SortMode: string
    EntryViewMode: string
    RequireConfirmationForLaunches: bool
    CustomEntryOrder: string list
    CommandTemplates: Map<string, string>
    SearchHistory: string list
    LastFilterText: string
    Window: LauncherWindowSettings
}

type LauncherPreset = {
    Id: string
    Name: string
    EntryIds: string list
    Locked: bool
}

type LauncherStoreFile = {
    SchemaVersion: int
    Settings: LauncherSettings
    Entries: LaunchEntry list
    Presets: LauncherPreset list
    DefaultPresetId: string
}

// QuoteArgument — port of LaunchArgumentBuilder.QuoteArgument
module LaunchArgumentBuilder =
    let quoteArgument (value: string) =
        let sb = System.Text.StringBuilder()
        sb.Append('"') |> ignore
        let mutable backslashes = 0
        for ch in value do
            if ch = '\\' then
                backslashes <- backslashes + 1
            elif ch = '"' then
                sb.Append('\\', backslashes * 2 + 1) |> ignore
                sb.Append('"') |> ignore
                backslashes <- 0
            else
                if backslashes > 0 then
                    sb.Append('\\', backslashes) |> ignore
                    backslashes <- 0
                sb.Append(ch) |> ignore
        if backslashes > 0 then
            sb.Append('\\', backslashes * 2) |> ignore
        sb.Append('"') |> ignore
        sb.ToString()
