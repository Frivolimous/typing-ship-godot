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
echo    5  Watch the bot play level 0
echo    6  Simulate level 0 at every difficulty  (fast, no window)
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
if "%PICK%"=="1" dotnet build && "%GODOT%" --path .
if "%PICK%"=="2" dotnet build && "%GODOT%" --path . --editor
if "%PICK%"=="3" dotnet build
if "%PICK%"=="4" "%GODOT%" --headless --path . --import
if "%PICK%"=="5" dotnet build && "%GODOT%" --path . -- --autotype
if "%PICK%"=="6" (
  dotnet build && for %%W in (25 40 65 75 90) do (
    echo   wpm %%W:
    "%GODOT%" --headless --fixed-fps 24 --path . -- --autotype --quit-on-end --wpm=%%W --seed=1 2>nul | findstr /b "Level"
  )
)
exit /b
