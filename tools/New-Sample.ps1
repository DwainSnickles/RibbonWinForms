<#
.SYNOPSIS
    Creates a new ribbon sample project inside this repository and adds it to RibbonLib.slnx.

.DESCRIPTION
    Everything mechanical is done here - project file, the reference that lets the out-of-process
    designer draw the ribbon, a form that already hosts a ribbon with a tab, a panel and a button,
    and the solution entry - so a change only has to touch what the request is actually about.

    The reference is a ProjectReference by default, which means edits to the library show up in the
    sample immediately. Pass -PackageVersion to consume the published package instead, which is how
    a consumer sees it.

.PARAMETER Name
    Project name, also the folder and namespace. Letters and digits only.

.PARAMETER Framework
    Target framework for the sample.

.PARAMETER PackageVersion
    Reference Dwain.RibbonLib at this version instead of the project. The package ships the design
    assembly in lib/, so no second reference is needed.

.PARAMETER Force
    Scaffold even if the folder already exists.

.PARAMETER SkipBuild
    Do not build the new project after scaffolding it.

.EXAMPLE
    .\tools\New-Sample.ps1 -Name RibbonGallery

.EXAMPLE
    .\tools\New-Sample.ps1 -Name RibbonFromPackage -PackageVersion 1.0.2-net10
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9]*$')]
    [string] $Name,

    [ValidateSet('net8.0-windows', 'net10.0-windows')]
    [string] $Framework = 'net10.0-windows',

    [string] $PackageVersion,

    [switch] $Force,

    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
$newline = [Environment]::NewLine

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'RibbonLib.slnx'
$relativeProject = "samples/$Name/$Name.csproj"
$sampleDir = Join-Path $root "samples\$Name"

if (-not (Test-Path (Join-Path $root 'RibbonLib.csproj'))) {
    throw "RibbonLib.csproj was not found in $root - this script belongs in the repository's tools folder."
}

if ((Test-Path $sampleDir) -and -not $Force) {
    throw "$sampleDir already exists. Pass -Force to scaffold over it, or pick another name."
}

# 1. Let the SDK create the project, so the template stays the SDK's responsibility.
Write-Host "Creating $Name ($Framework) in samples\$Name ..."
$newArgs = @('new', 'winforms', '-n', $Name, '-o', $sampleDir, '-f', ($Framework -replace '-windows$', ''))
if ($Force) { $newArgs += '--force' }   # dotnet new refuses a non-empty folder otherwise
& dotnet @newArgs | Out-Host
if ($LASTEXITCODE -ne 0) { throw "dotnet new winforms failed with exit code $LASTEXITCODE." }

# 2. Wire the ribbon. The designer assembly is copied next to the runtime assembly but never
#    compiled against: that is what the design server resolves so the ribbon draws at design time.
$csproj = Join-Path $sampleDir "$Name.csproj"
$referenceBlock = if ($PackageVersion) {
@"
  <ItemGroup>
    <!-- The published package, exactly as a consumer would get it. It carries RibbonLib.Design.dll
         in lib/<tfm>/, so the designer resolves without a second reference. -->
    <PackageReference Include="Dwain.RibbonLib" Version="$PackageVersion" />
  </ItemGroup>

"@
} else {
@"
  <ItemGroup>
    <ProjectReference Include="..\..\RibbonLib.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Copied to the output for the design server, but not a compile reference. -->
    <ProjectReference Include="..\..\RibbonLib.Design\RibbonLib.Design.csproj">
      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
      <OutputItemType>Content</OutputItemType>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </ProjectReference>
  </ItemGroup>

"@
}

$csprojText = Get-Content -Path $csproj -Raw
$csprojText = $csprojText -replace '(?s)</Project>\s*$', ($referenceBlock + '</Project>' + $newline)
Set-Content -Path $csproj -Value $csprojText -NoNewline

# 3. A form that already hosts the ribbon, written the way the designer generates it, so opening it
#    in Visual Studio and editing from there produces no surprises. 135px is the height at which a
#    single tab plus its panel band sits correctly.
#    Single-quoted here-strings: inside a double-quoted one PowerShell doubles the quotes in the
#    generated C#. __NAME__ is replaced after the fact instead.
$formText = @'
namespace __NAME__
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
    }
}
'@
Set-Content -Path (Join-Path $sampleDir 'Form1.cs') -Value $formText.Replace('__NAME__', $Name)

$designerText = @'
namespace __NAME__
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            ribbon = new RibbonLib.Ribbon();
            ribbonTab = new RibbonLib.RibbonTab();
            ribbonPanel = new RibbonLib.RibbonPanel();
            ribbonButton = new RibbonLib.RibbonButton();
            ((System.ComponentModel.ISupportInitialize)(ribbon)).BeginInit();
            SuspendLayout();
            // 
            // ribbon
            // 
            ribbon.Font = new Font("Segoe UI", 9F);
            ribbon.Location = new Point(0, 0);
            ribbon.Name = "ribbon";
            ribbon.Size = new Size(800, 135);
            ribbon.TabIndex = 0;
            ribbon.Tabs.Add(ribbonTab);
            ribbon.Text = "ribbon";
            // 
            // ribbonTab
            // 
            ribbonTab.Name = "ribbonTab";
            ribbonTab.Panels.Add(ribbonPanel);
            ribbonTab.Text = "Home";
            // 
            // ribbonPanel
            // 
            ribbonPanel.Items.Add(ribbonButton);
            ribbonPanel.Name = "ribbonPanel";
            ribbonPanel.Text = "Panel";
            // 
            // ribbonButton
            // 
            ribbonButton.Name = "ribbonButton";
            ribbonButton.Text = "Button";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(ribbon);
            Name = "Form1";
            Text = "__NAME__";
            ((System.ComponentModel.ISupportInitialize)(ribbon)).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private RibbonLib.Ribbon ribbon;
        private RibbonLib.RibbonTab ribbonTab;
        private RibbonLib.RibbonPanel ribbonPanel;
        private RibbonLib.RibbonButton ribbonButton;
    }
}
'@
Set-Content -Path (Join-Path $sampleDir 'Form1.Designer.cs') -Value $designerText.Replace('__NAME__', $Name)

# 4. Register it in the solution, keeping whatever is already there.
if (Test-Path $solution) {
    $slnxText = Get-Content -Path $solution -Raw
} else {
    $slnxText = "<Solution>$newline</Solution>$newline"
}

if ($slnxText -match [regex]::Escape($relativeProject)) {
    Write-Host "RibbonLib.slnx already lists $relativeProject."
} elseif ($slnxText -match '</Solution>') {
    $entry = "  <Project Path=""$relativeProject"" />$newline"
    $slnxText = $slnxText -replace '(?s)</Solution>', ($entry + '</Solution>')
    Set-Content -Path $solution -Value $slnxText -NoNewline
    Write-Host "Added $relativeProject to RibbonLib.slnx."
} else {
    Write-Warning "RibbonLib.slnx has no closing </Solution> tag; add $relativeProject by hand."
}

# 5. Build, so a broken scaffold fails here rather than in the designer.
if (-not $SkipBuild) {
    Write-Host 'Building the new sample ...'
    & dotnet build $csproj -v:q --nologo
    if ($LASTEXITCODE -ne 0) { throw "The new sample does not build (exit code $LASTEXITCODE)." }
}

Write-Host ''
Write-Host "Created $relativeProject"
Write-Host "  form:   samples\$Name\Form1.cs and Form1.Designer.cs"
Write-Host '  ribbon: one tab (Home), one panel, one button - extend it in the designer or in the file'
Write-Host '  next:   change only what the request needs; nothing else is set up by hand'
