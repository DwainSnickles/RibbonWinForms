<#
.SYNOPSIS
    Puts the ribbon in the Visual Studio toolbox of every Windows Forms project under a folder.

.DESCRIPTION
    For .NET (5 and later) Windows Forms, Visual Studio only deploys a toolbox control that comes
    from the open solution or from a NuGet package - a bare DLL cannot be added, and a VSIX cannot
    register one either (see the toolbox section of README.md). So "the ribbon in the toolbox of
    every project" means every project references the package, and this script does that in one
    pass instead of one `dotnet add package` per project.

    It looks at every *.csproj and *.vbproj under -Path (skipping bin, obj and .git), keeps only the
    ones that look like Windows Forms projects - <UseWindowsForms>true</UseWindowsForms>, or a
    target framework ending in -windows - and adds the PackageReference to each. Projects that
    already reference the package are left alone, so running it twice is harmless.

    The term "toolbox" is in the name because that is what it is for: restart Visual Studio (or
    reload the solution) afterwards and the RibbonLib Components tab is there for those projects.

.PARAMETER Path
    Folder to scan, or a single .csproj/.vbproj file. Defaults to the current folder.

.PARAMETER PackageId
    Package to add. Defaults to Dwain.RibbonLib.

.PARAMETER Version
    Package version. Defaults to 1.0.2-net10, the current prerelease on nuget.org - a prerelease
    version has to be given exactly, because it is not what a bare package id resolves to.

.PARAMETER Force
    Add the reference even to projects that do not look like Windows Forms projects.

.PARAMETER NoRestore
    Pass --no-restore to dotnet add package. Faster, but the package is not downloaded, so the
    toolbox tab will not appear until the project is restored/built.

.EXAMPLE
    .\tools\Add-RibbonToolbox.ps1

    Adds the ribbon to every WinForms project in the repository.

.EXAMPLE
    .\tools\Add-RibbonToolbox.ps1 -Path D:\Work\MySuite -NoRestore

    Points at another solution folder and leaves restoring to the next build.

.EXAMPLE
    .\tools\Add-RibbonToolbox.ps1 -WhatIf

    Lists what would change without touching a project file.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Position = 0)]
    [string] $Path = '.',

    [string] $PackageId = 'Dwain.RibbonLib',

    [string] $Version = '1.0.2-net10',

    [switch] $Force,

    [switch] $NoRestore
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK was not found on PATH: dotnet add package is how the reference is written.'
}

$resolved = Resolve-Path -LiteralPath $Path -ErrorAction SilentlyContinue

if (-not $resolved) {
    throw "Not found: $Path"
}

$item = Get-Item -LiteralPath $resolved

if ($item.PSIsContainer) {
    $projects = @(Get-ChildItem -LiteralPath $item.FullName -Recurse -File |
        Where-Object { $_.Extension -in '.csproj', '.vbproj' } |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.git)[\\/]' } |
        Sort-Object FullName)
}
else {
    $projects = @($item)
}

if ($projects.Count -eq 0) {
    Write-Host "No .csproj or .vbproj files under $($item.FullName)." -ForegroundColor Yellow
    return
}

$added = 0
$already = 0
$skipped = 0
$failed = 0

foreach ($project in $projects) {
    $name = $project.Name
    $text = Get-Content -LiteralPath $project.FullName -Raw

    # Already there: a package reference, however it is spelled or quoted.
    if ($text -match "(?i)<PackageReference\s[^>]*Include\s*=\s*[`"']$([regex]::Escape($PackageId))[`"']") {
        Write-Host "  has it      $name" -ForegroundColor DarkGray
        $already++
        continue
    }

    $isWinForms = $text -match '(?i)<UseWindowsForms>\s*true\s*</UseWindowsForms>' -or
                  $text -match '(?i)<TargetFrameworks?>\s*[^<]*-windows'

    if (-not $isWinForms -and -not $Force) {
        Write-Host "  not WinForms $name" -ForegroundColor DarkGray
        $skipped++
        continue
    }

    if (-not $PSCmdlet.ShouldProcess($name, "add $PackageId $Version")) {
        continue
    }

    $arguments = @('add', $project.FullName, 'package', $PackageId, '--version', $Version)

    if ($NoRestore) {
        $arguments += '--no-restore'
    }

    $output = & dotnet @arguments 2>&1
    $exit = $LASTEXITCODE

    if ($exit -ne 0) {
        Write-Host "  FAILED      $name" -ForegroundColor Red
        $output | ForEach-Object { Write-Host "              $_" -ForegroundColor Red }
        $failed++
        continue
    }

    Write-Host "  added       $name" -ForegroundColor Green
    $added++
}

Write-Host ''
Write-Host ("{0} added, {1} already had it, {2} not WinForms, {3} failed." -f
    $added, $already, $skipped, $failed)

if ($added + $already -gt 0) {
    Write-Host ''
    Write-Host 'Restart Visual Studio (or right-click the toolbox and choose Refresh) after a restore,'
    Write-Host 'then open a form: the RibbonLib Components tab holds the Ribbon control.'
}

if ($failed -gt 0) {
    exit 1
}
