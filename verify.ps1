#Requires -Version 5.0
<#
  一键验收脚本（本机口径）：
    [1/2] 主工程 MSBuild Rebuild（Cognex VisionPro 依赖，只能本机编）
    [2/2] 单元测试 dotnet test（源码链接工程，无第三方依赖）
  用法：
    .\verify.ps1                 # Debug：先编主工程再跑测试（完整验收）
    .\verify.ps1 -Configuration Release
    .\verify.ps1 -SkipMainBuild  # 只跑测试（CI 托管 runner 同款口径）
  退出码：0 = 全部通过；非 0 = 失败步骤的退出码。
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$SkipMainBuild
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($found) { return $found }
    }
    $fallback = 'D:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe'
    if (Test-Path $fallback) { return $fallback }
    throw 'MSBuild 未找到：vswhere 与默认路径均失败，请检查 VS 安装。'
}

if (-not $SkipMainBuild) {
    $msbuild = Find-MSBuild
    Write-Host "== [1/2] 主工程 Rebuild ($Configuration) =="
    & $msbuild (Join-Path $root 'WindowsFormsApplication1\WindowsFormsApplication1.csproj') `
        /t:Rebuild "/p:Configuration=$Configuration" /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "主工程编译失败，exit=$LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
else {
    Write-Host '== [1/2] 跳过主工程编译（-SkipMainBuild） =='
}

Write-Host "== [2/2] 单元测试 ($Configuration) =="
dotnet test (Join-Path $root 'WindowsFormsApplication1.Tests\WindowsFormsApplication1.Tests.csproj') `
    -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "单元测试失败，exit=$LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host 'verify.ps1：主工程编译 + 单元测试 全部通过。' -ForegroundColor Green
exit 0
