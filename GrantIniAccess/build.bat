@echo off
cd /d "%~dp0"
msbuild GrantIniAccess.csproj /p:Configuration=Release /p:Platform=AnyCPU /v:minimal
if %ERRORLEVEL% NEQ 0 (
    echo 编译失败
    pause
    exit /b 1
)
echo.
echo 编译成功: %~dp0bin\Release\GrantIniAccess.exe
pause
