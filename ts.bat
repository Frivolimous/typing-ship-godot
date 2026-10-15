@echo off
rem Millennium Typer: one menu for every everyday command. Double-click it, or run "ts" from the repo root.
rem "ts 3" runs menu option 3 straight away. docs\commands.md explains each option.
setlocal EnableExtensions
cd /d "%~dp0"
if "%GODOT%"=="" set "GODOT=C:\Tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"

if not "%~1"=="" (
  set "PICK=%~1"
  call :run
  exit /b
)

:menu
cls
echo.
echo   Millennium Typer
echo   ----------------
echo    1  Play the game
echo    2  Open the Godot editor
echo.
echo    3  Build the C#
echo    4  Re-import assets
echo.
echo    5  Watch the bot play a level
echo    6  Simulate a level at every difficulty  (fast, no window)
echo    7  Simulate every level at every difficulty
echo.
echo    8  Edit a level: play it with the editing keys
echo    9  Check the level and tuning files
echo   10  Run the tests
echo   11  Level pacing report, game levels and Classic  (charts and CSVs in pacing\)
echo   12  Play the Classic levels  (the original 12 as they were; own save file)
echo   13  Check the pacing rules  (every rule a game level breaks; docs\pacing-rules.md)
echo.
echo    0  Quit
echo.
set "PICK="
set /p "PICK=  Pick: "
if "%PICK%"=="0" exit /b
call :run
echo.
pause
goto menu

:run
if "%PICK%"=="1" dotnet build && "%GODOT%" --path game
if "%PICK%"=="2" dotnet build && "%GODOT%" --path game --editor
if "%PICK%"=="3" dotnet build
if "%PICK%"=="4" "%GODOT%" --headless --path game --import
if "%PICK%"=="5" goto watch
if "%PICK%"=="6" goto simone
if "%PICK%"=="7" goto simall
if "%PICK%"=="8" goto edit
if "%PICK%"=="9" dotnet build && "%GODOT%" --headless --path game -- --check 2>nul | findstr /v /b /c:"Godot Engine"
if "%PICK%"=="10" dotnet test
if "%PICK%"=="11" dotnet run --project src\Sim -- pacing && start "" "pacing\pacing.html"
if "%PICK%"=="12" dotnet build && "%GODOT%" --path game -- --classic --profile=classic
if "%PICK%"=="13" dotnet run --project src\Sim -- pacing --check
exit /b

:watch
call :asklevel
dotnet build && "%GODOT%" --path game -- --autotype --level=%LVL%
exit /b

:simone
call :asklevel
dotnet build && call :sim %LVL%
exit /b

:simall
dotnet build && for /l %%L in (0,1,11) do call :sim %%L
exit /b

:edit
call :asklevel
set "START="
set /p "START=  Start at (Enter = the beginning, 50 = 50%% of the way in, L40 = line 40 of the file): "
set "FROM="
if not "%START%"=="" (
  if /i "%START:~0,1%"=="L" (set "FROM=--line=%START:~1%") else (set "FROM=--from=%START%")
)
set "BOT="
set /p "BOT=  Let the bot play? (y/N): "
set "AUTO="
if /i "%BOT%"=="y" set "AUTO=--autotype"
set "WPM="
set /p "WPM=  Difficulty in WPM (Enter = 40, Normal): "
if "%WPM%"=="" set "WPM=40"
dotnet build && "%GODOT%" --path game -- --edit --overlay --level=%LVL% --wpm=%WPM% %FROM% %AUTO%
exit /b

:asklevel
set "LVL=0"
set /p "LVL=  Level (0-11, Enter for 0): "
exit /b

:sim
for %%W in (25 40 65 75 90) do (
  echo   level %1, wpm %%W:
  "%GODOT%" --headless --fixed-fps 24 --path game -- --autotype --quit-on-end --level=%1 --wpm=%%W --seed=1 2>nul | findstr /b "Level"
)
exit /b
