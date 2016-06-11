param (
    [Parameter()]
    [string] $Configuration = "Release"
)

$watch = [System.Diagnostics.Stopwatch]::StartNew()
$scriptPath = Split-Path (Get-Variable MyInvocation).Value.MyCommand.Path 
Push-Location $scriptPath
$productName = "Scribe";
$destination = "C:\Binaries\$productName"
$nugetDestination = "C:\Workspaces\Nuget\Developer"

if (Test-Path $destination -PathType Container){
    Remove-Item $destination -Recurse -Force
}

New-Item $destination -ItemType Directory | Out-Null
New-Item $destination\bin -ItemType Directory | Out-Null

if (!(Test-Path $nugetDestination -PathType Container)){
    New-Item $nugetDestination -ItemType Directory | Out-Null
}

.\IncrementVersion.ps1 -Build +

& nuget.exe restore "$scriptPath\$productName.sln"
$msbuild = "C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"
& $msbuild "$scriptPath\$productName.sln" /p:Configuration="$Configuration" /p:Platform="Any CPU" /p:PublishProfile=Release /p:DeployOnBuild=True /t:Rebuild /p:VisualStudioVersion=14.0 /v:m /m

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build has failed! " $watch.Elapsed -ForegroundColor Red
    exit $LASTEXITCODE
}

$versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptPath\Scribe\bin\$Configuration\Scribe.dll")
$build = ([Version] $versionInfo.ProductVersion).Build
$version = $versionInfo.FileVersion.Replace(".$build.0", ".$build")

Copy-Item Scribe\bin\$Configuration\Scribe.dll $destination\bin\

& "nuget.exe" pack Scribe.nuspec -Prop Configuration="$Configuration" -Version $version
Move-Item "Scribe.Wiki.$version.nupkg" "$destination" -force
Copy-Item "$destination\$productName.Wiki.$version.nupkg" "$nugetDestination" -force

Write-Host
Write-Host "$productName Build: " $watch.Elapsed -ForegroundColor Yellow
Pop-Location