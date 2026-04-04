# Run on Windows: .\package-win.ps1 [-Arch x64|arm64]
param(
    [ValidateSet("x64", "arm64")]
    [string]$Arch = "x64"
)

$ErrorActionPreference = "Stop"

$AppName     = "QuillStone"
$AppVersion  = "1.0.0"
$Manufacturer = "bopera"
$UpgradeCode = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890"  # never change this

$Rid        = "win-$Arch"
$PublishDir = "QuillStone\bin\Release\net10.0\$Rid\publish"
$OutDir     = "$AppName-win-$Arch"
$Msi        = "$AppName-win-$Arch.msi"

Write-Host "Publishing for $Rid..."
dotnet publish QuillStone\QuillStone.csproj -c Release -r $Rid --self-contained true

Write-Host "Staging files..."
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
Copy-Item $PublishDir $OutDir -Recurse

Write-Host "Checking WiX..."
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host "Installing WiX Toolset..."
    dotnet tool install --global wix
    wix extension add WixToolset.UI.wixext
}

$WxsMain  = [System.IO.Path]::GetTempFileName() + ".wxs"
$WxsFiles = [System.IO.Path]::GetTempFileName() + ".wxs"

Write-Host "Harvesting files..."
wix harvest dir $OutDir -cg AppFiles -dr INSTALLFOLDER -var var.SourceDir -out $WxsFiles

Write-Host "Generating product definition..."
@"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui">
  <Package Name="$AppName" Version="$AppVersion" Manufacturer="$Manufacturer"
           UpgradeCode="$UpgradeCode" Language="1033">

    <MajorUpgrade DowngradeErrorMessage="A newer version of $AppName is already installed." />
    <MediaTemplate EmbedCab="yes" />
    <ui:WixUI Id="WixUI_Minimal" />

    <Feature Id="Main" Title="$AppName" Level="1">
      <ComponentGroupRef Id="AppFiles" />
      <ComponentRef Id="AppShortcut" />
    </Feature>

    <StandardDirectory Id="ProgramFilesFolder">
      <Directory Id="INSTALLFOLDER" Name="$AppName" />
    </StandardDirectory>

    <StandardDirectory Id="DesktopFolder">
      <Component Id="AppShortcut" Guid="*">
        <Shortcut Id="DesktopShortcut" Name="$AppName"
                  Target="[INSTALLFOLDER]$AppName.exe"
                  WorkingDirectory="INSTALLFOLDER" />
        <RemoveFolder Id="RemoveDesktopFolder" On="uninstall" />
        <RegistryValue Root="HKCU" Key="Software\$Manufacturer\$AppName"
                       Name="installed" Type="integer" Value="1" KeyPath="yes" />
      </Component>
    </StandardDirectory>

  </Package>
</Wix>
"@ | Set-Content $WxsMain -Encoding UTF8

Write-Host "Building MSI..."
wix build $WxsMain $WxsFiles -d "SourceDir=$OutDir" -ext WixToolset.UI.wixext -o $Msi

Remove-Item $WxsMain, $WxsFiles -Force
Remove-Item $OutDir -Recurse -Force

Write-Host "Done! Run $Msi to install $AppName."
