@ECHO OFF
CLS

IF NOT "%1"=="" (
  SET ARCH=%1
) ELSE (
  SET ARCH=x86
)

Title Building Trakt (RELEASE)
CD ..

REM Select program path based on current machine environment
SET progpath=%ProgramFiles%
IF NOT "%ProgramFiles(x86)%".=="". SET progpath=%ProgramFiles(x86)%

REM Define MSbuild path
SET MSBUILD_PATH=%progpath%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe
IF NOT EXIST "%MSBUILD_PATH%" SET MSBUILD_PATH=%progpath%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe
IF NOT EXIST "%MSBUILD_PATH%" SET MSBUILD_PATH=%progpath%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\bin\MSBuild.exe
IF NOT EXIST "%MSBUILD_PATH%" SET MSBUILD_PATH=%progpath%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\bin\MSBuild.exe
IF NOT EXIST "%MSBUILD_PATH%" SET MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe
IF NOT EXIST "%MSBUILD_PATH%" SET MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe
IF NOT EXIST "%MSBUILD_PATH%" SET MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\bin\MSBuild.exe
IF NOT EXIST "%MSBUILD_PATH%" SET MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\bin\MSBuild.exe

REM Prepare version
FOR /f "tokens=*" %%a in ('git rev-list HEAD --count') DO SET REVISION=%%a 
SET REVISION=%REVISION: =%
SET /A REVISION=REVISION-1000
"Tools\Tools\sed.exe" -i -r "s/(Assembly(File)?Version\(.[0-9]+\.[0-9]+\.[0-9]+\.)[0-9]+(.\))/\1%REVISION%\3/g" "TraktPlugin\Properties\AssemblyInfo.cs"

REM Prepare Client ID
IF DEFINED CLIENT_ID (
  ECHO [INFO] Client ID found...
  "Tools\Tools\sed.exe" -i -r "s/SECRET_PLACEHOLDER_CLIENT_ID/%CLIENT_ID%/g" "TraktPlugin\TraktCredentials.cs"
)

REM Prepare Client Token
IF DEFINED CLIENT_SECRET (
  ECHO [INFO] Client Token found...
  "Tools\Tools\sed.exe" -i -r "s/SECRET_PLACEHOLDER_CLIENT_SECRET/%CLIENT_SECRET%/g" "TraktPlugin\TraktCredentials.cs"
)

REM Build
"%MSBUILD_PATH%" /target:Rebuild /property:Configuration=RELEASE /property:Platform=%ARCH% /fl /flp:logfile=Trakt.log;verbosity=diagnostic TraktPlugin.sln

REM Revert version
git checkout "TraktPlugin\Properties\AssemblyInfo.cs"
git checkout "TraktPlugin\TraktCredentials.cs"

CD build
PAUSE
