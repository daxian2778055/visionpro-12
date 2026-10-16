param(
    [string]$AssemblyPath = ''
)
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path -Parent $PSScriptRoot
if (-not $AssemblyPath) {
    # No -AssemblyPath given: probe the historical default first (x64/Debug), then the rest,
    # so a manual run keeps working without arguments.
    foreach ($rel in @(
        'WindowsFormsApplication1\bin\x64\Debug\WindowsFormsApplication1.exe',
        'WindowsFormsApplication1\bin\x64\Release\WindowsFormsApplication1.exe',
        'WindowsFormsApplication1\bin\Debug\WindowsFormsApplication1.exe',
        'WindowsFormsApplication1\bin\Release\WindowsFormsApplication1.exe')) {
        $candidate = Join-Path $repoPath $rel
        if (Test-Path -LiteralPath $candidate) { $AssemblyPath = $candidate; break }
    }
}
if (-not $AssemblyPath -or -not (Test-Path -LiteralPath $AssemblyPath)) {
    throw 'WindowsFormsApplication1.exe not found. Build the main project first, or pass -AssemblyPath.'
}
$compilerPath = ''
$msbuild = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
if ($msbuild) { $compilerPath = Join-Path (Split-Path $msbuild.Source) 'Roslyn\csc.exe' }
if (-not $compilerPath -or -not (Test-Path -LiteralPath $compilerPath)) {
    # MSBuild.exe not in PATH: fall back to vswhere (same fallback as verify.ps1).
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($found) { $compilerPath = Join-Path (Split-Path $found) 'Roslyn\csc.exe' }
    }
}
if (-not $compilerPath -or -not (Test-Path -LiteralPath $compilerPath)) {
    throw 'csc.exe not found (MSBuild/Roslyn). Install VS 2022 MSBuild.'
}
$outputDirectory = Join-Path $PSScriptRoot 'bin'
[void][IO.Directory]::CreateDirectory($outputDirectory)
$testAssemblyPath = Join-Path $outputDirectory 'InspectionRegressionTests.dll'
& $compilerPath /nologo /target:library /platform:x64 "/out:$testAssemblyPath" "/reference:$AssemblyPath" (Join-Path $PSScriptRoot 'InspectionRegressionTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Regression test compilation failed.' }
[void][Reflection.Assembly]::LoadFrom($AssemblyPath)
[void][Reflection.Assembly]::LoadFrom($testAssemblyPath)
[InspectionRegressionTests]::Run()
