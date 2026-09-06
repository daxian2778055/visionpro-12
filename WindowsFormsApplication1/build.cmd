@echo off
setlocal
set "PROJDIR=%~dp0"
set "CSPROJ=%PROJDIR%WindowsFormsApplication1.csproj"

rem Known-good MSBuild path (VS2022 Professional); vswhere only as fallback
set "MSBUILD=D:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%MSBUILD%" (
  if exist "%VSWHERE%" (
    for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\Current\Bin\MSBuild.exe"`) do set "MSBUILD=%%i"
  )
)
if not exist "%MSBUILD%" (
  echo MSBuild.exe not found. Install VS2022 with MSBuild or fix the fallback path.
  exit /b 2
)

set "LOG=%PROJDIR%build.log"
echo MSBuild: %MSBUILD%
echo Building: %CSPROJ%
"%MSBUILD%" "%CSPROJ%" /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal /nologo "/flp:logfile=%LOG%;verbosity=minimal"
set "RC=%ERRORLEVEL%"

for /f %%a in ('findstr /i /c:": error " "%LOG%" ^| find /c /v ""') do set "ERRCOUNT=%%a"
for /f %%a in ('findstr /i /c:": warning " "%LOG%" ^| find /c /v ""') do set "WARNCOUNT=%%a"

echo.
echo ==== Build summary ====
echo MSBuild exit code : %RC%
echo Errors (approx)   : %ERRCOUNT%
echo Warnings (approx) : %WARNCOUNT%
if "%RC%"=="0" (echo BUILD OK - 0 errors) else (echo BUILD FAILED)
exit /b %RC%
