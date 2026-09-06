@echo off
setlocal EnableDelayedExpansion
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

set "LOG=%PROJDIR%build_release.log"
echo MSBuild: %MSBUILD%
echo Building (Release + PDB): %CSPROJ%
rem DebugType=portable + DebugSymbols=true 强制生成 PDB，不依赖 csproj 默认（部分旧模板 Release 不生成）
"%MSBUILD%" "%CSPROJ%" /t:Build /p:Configuration=Release /p:Platform=AnyCPU /p:DebugType=portable /p:DebugSymbols=true /v:minimal /nologo "/flp:logfile=%LOG%;verbosity=minimal"
set "RC=%ERRORLEVEL%"

rem ==== 符号留存：把 PDB 归档到 SymbolsArchive，不随 exe 发布 ====
rem 客户机上的 *.dmp 拿回来后，用同版本 PDB 即可在 VS/Windbg 还原到源码行
set "SYMDIR=%PROJDIR%SymbolsArchive"
if not exist "%SYMDIR%" mkdir "%SYMDIR%"
if exist "%PROJDIR%bin\Release\WindowsFormsApplication1.pdb" (
  for /f %%i in ('wmic os get localdatetime ^| find "."') do set "LDT=%%i"
  set "TS=!LDT:~0,14!"
  copy /Y "%PROJDIR%bin\Release\WindowsFormsApplication1.pdb" "%SYMDIR%\WindowsFormsApplication1_!TS!.pdb" >nul
  echo PDB archived -^> %SYMDIR%\WindowsFormsApplication1_!TS!.pdb
) else (
  echo [WARN] PDB not found, symbol archive skipped.
)

for /f %%a in ('findstr /i /c:": error " "%LOG%" ^| find /c /v ""') do set "ERRCOUNT=%%a"
for /f %%a in ('findstr /i /c:": warning " "%LOG%" ^| find /c /v ""') do set "WARNCOUNT=%%a"

echo.
echo ==== Build summary (Release) ====
echo MSBuild exit code : %RC%
echo Errors (approx)   : %ERRCOUNT%
echo Warnings (approx) : %WARNCOUNT%
if "%RC%"=="0" (echo BUILD OK - 0 errors) else (echo BUILD FAILED)
exit /b %RC%
