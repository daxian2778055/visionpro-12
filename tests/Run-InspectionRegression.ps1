$ErrorActionPreference = 'Stop'
$repoPath = Split-Path -Parent $PSScriptRoot
$assemblyPath = Join-Path $repoPath 'WindowsFormsApplication1\bin\x64\Debug\WindowsFormsApplication1.exe'
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw 'Build Debug/x64 before running the regression tests.'
}
$compilerPath = Join-Path (Split-Path (Get-Command MSBuild.exe).Source) 'Roslyn\csc.exe'
$outputDirectory = Join-Path $PSScriptRoot 'bin'
[void][IO.Directory]::CreateDirectory($outputDirectory)
$testAssemblyPath = Join-Path $outputDirectory 'InspectionRegressionTests.dll'
& $compilerPath /nologo /target:library /platform:x64 "/out:$testAssemblyPath" "/reference:$assemblyPath" (Join-Path $PSScriptRoot 'InspectionRegressionTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Regression test compilation failed.' }
[void][Reflection.Assembly]::LoadFrom($assemblyPath)
[void][Reflection.Assembly]::LoadFrom($testAssemblyPath)
[InspectionRegressionTests]::Run()
