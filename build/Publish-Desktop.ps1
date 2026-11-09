param([string]$Version='1.8.0')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
$output=Join-Path $root 'artifacts/desktop'
dotnet publish ViVe.Desktop/ViVe.Desktop.csproj -c Release -r win-x64 --self-contained true -p:Version=$Version -o $output --nologo
if($LASTEXITCODE){throw 'Desktop publish failed.'}
Copy-Item LICENSE,README.md,COPYRIGHT,THIRD-PARTY-NOTICES.md $output
Copy-Item Extra/DICTIONARY-SOURCES.md $output
New-Item -ItemType Directory -Force "$output/licenses/dotnet" | Out-Null
$runtime=Get-ChildItem "$env:DOTNET_ROOT/shared/Microsoft.NETCore.App" -Directory | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
foreach($file in @('LICENSE.txt','ThirdPartyNotices.txt')) {if(Test-Path "$($runtime.FullName)/$file"){Copy-Item "$($runtime.FullName)/$file" "$output/licenses/dotnet/"}}
New-Item -ItemType Directory -Force artifacts/release | Out-Null
Compress-Archive -Path "$output/*" -DestinationPath "artifacts/release/ViVe-Desktop-$Version-win-x64.zip" -Force
