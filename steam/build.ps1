# Millennium Typer release builds: exports the game with Godot's release templates (no dev options or dev keys),
# checks the exported build reads its embedded data, and can upload it to Steam with SteamPipe.
#
#   powershell -ExecutionPolicy Bypass -File steam\build.ps1                     Windows build in builds\windows
#   powershell -ExecutionPolicy Bypass -File steam\build.ps1 -Linux              and Linux in builds\linux
#   powershell -ExecutionPolicy Bypass -File steam\build.ps1 -Upload main        upload to the main app's beta branch
#   ... -Upload main -Branch testers -Description "endless tweaks"               another branch, a note for the build
#   ... -Upload playtest                                                         upload to the Steam Playtest app
#
# ts 17 and ts 18 run the first and third. steam\README.md explains the Steam side (branches, depots, steamcmd).
param(
    [ValidateSet("none", "main", "playtest")] [string]$Upload = "none",
    [string]$Branch = "beta",
    [string]$Description = "",
    [switch]$Linux
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

# The Steamworks apps and their depots (partner site, SteamPipe > Depots). Linux depots don't exist yet: add one
# (operating system: Linux) and put its ID here to upload -Linux builds; Steam Deck runs the Windows build meanwhile.
$apps = @{
    main     = @{ App = 5450910; Windows = 5450911; Linux = 0 }
    playtest = @{ App = 5490840; Windows = 5490841; Linux = 0 }
    # demo   = @{ App = 5490830; Windows = 5490831; Linux = 0 }   # once there is a demo build
}

$godot = if ($env:GODOT) { $env:GODOT } else { "C:\Tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" }
$builds = Join-Path $root "builds"
$platforms = @(@{ Name = "Windows"; Folder = "windows"; Exe = "MillenniumTyper.exe" })
if ($Linux) { $platforms += @{ Name = "Linux"; Folder = "linux"; Exe = "MillenniumTyper.x86_64" } }

# Music is git-ignored, so only Jeremy's machine has it: a build without it is silent.
$music = @(Get-ChildItem "game\assets\music" -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in ".wav", ".ogg", ".mp3" })
if ($music.Count -eq 0) {
    if ($Upload -ne "none") { throw "No music in game\assets\music: don't upload a silent build. Build on the machine that has the music." }
    Write-Warning "No music in game\assets\music: this build will be silent."
}

Write-Host "== Building the C#"
dotnet build
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

foreach ($p in $platforms) {
    $folder = Join-Path $builds $p.Folder
    Write-Host "== Exporting $($p.Name) to $folder"
    if (Test-Path $folder) { Remove-Item $folder -Recurse -Force }
    New-Item -ItemType Directory -Force $folder | Out-Null
    & $godot --headless --path game --export-release $p.Name "../builds/$($p.Folder)/$($p.Exe)"
    if ($LASTEXITCODE -ne 0 -or !(Test-Path (Join-Path $folder $p.Exe))) { throw "Godot couldn't export $($p.Name)" }
}

# The exported game must read the data files embedded in its assembly (there's no data\ folder next to it).
Write-Host "== Checking the Windows build reads its data"
$out = Join-Path $builds "check.txt"
$run = Start-Process (Join-Path $builds "windows\MillenniumTyper.exe") -ArgumentList "--headless", "--", "--check" -NoNewWindow -Wait -PassThru -RedirectStandardOutput $out
$check = Get-Content $out -Raw
Remove-Item $out
if ($run.ExitCode -ne 0 -or $check -notmatch "Data OK.*\(embedded\)") { throw "The exported build failed its data check:`n$check" }
Write-Host ($check -split "`n" | Where-Object { $_ -match "Data OK" })

if ($Upload -eq "none") {
    Write-Host "== Done: builds\windows$(if ($Linux) { ' and builds\linux' })"
    exit 0
}

# SteamPipe: an app build script naming each depot's content, written fresh for every upload.
$app = $apps[$Upload]
$out = Join-Path $builds "steam"
New-Item -ItemType Directory -Force $out | Out-Null
$depots = @{ $app.Windows = "windows" }
if ($Linux) {
    if ($app.Linux -eq 0) { throw "No Linux depot for the $Upload app yet: add one on the partner site and put its ID in steam\build.ps1." }
    $depots[$app.Linux] = "linux"
}
$depotLines = foreach ($id in $depots.Keys) {
    $depotFile = Join-Path $out "depot_$id.vdf"
    @"
"DepotBuild"
{
    "DepotID" "$id"
    "ContentRoot" "$(Join-Path $builds $depots[$id])"
    "FileMapping"
    {
        "LocalPath" "*"
        "DepotPath" "."
        "Recursive" "1"
    }
}
"@ | Set-Content $depotFile -Encoding ascii
    "        `"$id`" `"$depotFile`""
}
# Steam won't let a build go live on the default branch from here: for Playtest (and the release), set it live
# on the partner site (SteamPipe > Builds).
$setLive = if ($Upload -eq "main") { $Branch } else { "" }
$git = (git rev-parse --short HEAD 2>$null)
$desc = "Millennium Typer $git $Description".Trim()
$appFile = Join-Path $out "app_$($app.App).vdf"
@"
"AppBuild"
{
    "AppID" "$($app.App)"
    "Desc" "$desc"
    "BuildOutput" "$(Join-Path $out 'output')"
    "SetLive" "$setLive"
    "Depots"
    {
$($depotLines -join "`n")
    }
}
"@ | Set-Content $appFile -Encoding ascii

# steamcmd: $env:STEAMCMD, or Valve's download kept in steam\steamcmd (git-ignored).
$steamcmd = if ($env:STEAMCMD) { $env:STEAMCMD } else { Join-Path $PSScriptRoot "steamcmd\steamcmd.exe" }
if (!(Test-Path $steamcmd)) {
    Write-Host "== Downloading steamcmd from Valve"
    $dir = Split-Path $steamcmd
    New-Item -ItemType Directory -Force $dir | Out-Null
    $zip = Join-Path $dir "steamcmd.zip"
    Invoke-WebRequest "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" -OutFile $zip
    Expand-Archive $zip $dir -Force
    Remove-Item $zip
}
$user = if ($env:STEAM_USER) { $env:STEAM_USER } else { Read-Host "Steam login (an account with SteamPipe rights on the app)" }
Write-Host "== Uploading to app $($app.App)$(if ($setLive) { ", live on the '$setLive' branch" }) (steamcmd asks for the password and Steam Guard code)"
& $steamcmd +login $user +run_app_build $appFile +quit
if ($LASTEXITCODE -ne 0) { throw "steamcmd failed (exit $LASTEXITCODE): see the log above and $out\output" }
Write-Host "== Uploaded: $desc"
