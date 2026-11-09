# Chest Tracker 3.0

Windows 10 2004+ / Windows 11, x64. Build with the .NET 10 SDK:

```powershell
dotnet publish ChestTracker.csproj -c Release -r win-x64 --self-contained true -m:1 -p:UseSharedCompilation=false -o ../publish
```

The result is one self-contained `ChestTracker.exe`. No Python, separate .NET
runtime, cloud service or Epic account token is required on the destination PC.
Third-party notices, map art and chest coordinates are embedded resources;
notices can be viewed from the application. No companion files are required.

Verification (use a scratch folder for reports):

```powershell
../publish/ChestTracker.exe --self-test C:/scratch/tests.txt
../publish/ChestTracker.exe --verify-replay C:/scratch/sample.replay C:/scratch/integration.txt
```

`--analyze input.replay output.json` is the isolated parsing worker, also useful
for diagnosing supported replay versions. `--preview C:/scratch/demo-data`
shows explicitly labeled sample data and disables observation.

The UI and state model are in the root C# files. `Parser/ReplayAnalyzer.cs`,
`Parser/Lang.cs` and the model definitions are derived from
https://github.com/LED-N/Fortnite-ChestCounter under MIT. `lib/` contains its
vendored Shiqan/FortniteReplayDecompressor fork, including its upstream license
and patch notes. See the distributed `licenses/` directory.

Changes here: a WPF interface in Russian; ordinary/rare-only filtering with
spirit/other-container exclusions; opened and destroyed counts; isolated worker
with timeout and heap limit; portable atomic history and backup; replay dedupe;
one-time migration; CSV; optional local lobby OCR and spirit reminder; tests.

Replay type names may change with Fortnite updates. Unrecognized types are
excluded, and parse/attribution errors are reported instead of producing a
successful zero match. The filter follows the requested categories and does
not validate Fortnite's server-side token reward probabilities.

All runtime state is in `%LOCALAPPDATA%/ChestTrackerDesktop`, automatically
migrated from known v2 portable locations. UTC timestamps are normalized at the
replay boundary, converted to the current Windows time zone for display, and
the v2 unspecified-kind bug is repaired once with a backup. UI language is Russian;
lobby OCR accepts Russian/English menu labels when the corresponding Windows
OCR language is installed. Detection samples only the active Fortnite window's
top quarter while a reminder is pending, never uploads images, and suspends
while a replay is actively recording.

The native interactive map embeds season 43 Fortnite.gg map art and 865 regular /
39 rare chest coordinates from the public snapshot in
https://github.com/CheesyPoofs346/chestlab/tree/main/assets/gg . No ChestLab
program code is used. Attribution remains Fortnite.gg / Epic Games. Rendering
and clustering happen only while the map is used; it does not track a live player.
The reward catalog is a verified snapshot of https://fortnite.gg/treasure-hunt
on 2026-11-09. Only weeks 1–2 are published in that snapshot; future lists are
explicitly unknown and the source link is available for newer information.
