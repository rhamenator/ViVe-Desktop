param([string]$Version='1.8.0.0',[string]$Identity='rhamenator.ViVeDesktop.Test',[string]$Publisher='CN=ViVe Desktop Test',[string]$PublisherDisplayName='Richard M. Hamilton')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
if(!$Identity){$Identity='rhamenator.ViVeDesktop.Test'}
if(!$Publisher){$Publisher='CN=ViVe Desktop Test'}
if(!$PublisherDisplayName){$PublisherDisplayName='Richard M. Hamilton'}
$stage=Join-Path $root 'artifacts/msix-stage'
$output=Join-Path $root 'artifacts/store'
New-Item -ItemType Directory -Force $stage,$output,"$stage/Assets" | Out-Null
Copy-Item artifacts/desktop/* $stage -Recurse -Force
Add-Type -AssemblyName System.Drawing
$original=[Drawing.Image]::FromFile((Join-Path $root 'ViVe.Desktop/Assets/App.png'))
foreach($size in @(44,50,150)) {
 $bitmap=[Drawing.Bitmap]::new($size,$size);$graphics=[Drawing.Graphics]::FromImage($bitmap)
 $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $graphics.DrawImage($original,0,0,$size,$size);$bitmap.Save("$stage/Assets/Logo$size.png",[Drawing.Imaging.ImageFormat]::Png)
 $graphics.Dispose();$bitmap.Dispose()
}
$original.Dispose()
$manifest=[xml](Get-Content packaging/Package.appxmanifest -Raw)
$manifest.Package.Identity.Name=$Identity;$manifest.Package.Identity.Publisher=$Publisher;$manifest.Package.Identity.Version=$Version
$manifest.Package.Properties.PublisherDisplayName=$PublisherDisplayName
$manifest.Save("$stage/AppxManifest.xml")
$sdk=Get-ChildItem "${env:ProgramFiles(x86)}/Windows Kits/10/bin" -Directory | Sort-Object Name -Descending | Where-Object {Test-Path "$($_.FullName)/x64/makeappx.exe"} | Select-Object -First 1
if(!$sdk){throw 'Windows SDK MakeAppx was not found.'}
$makeappx="$($sdk.FullName)/x64/makeappx.exe";$signtool="$($sdk.FullName)/x64/signtool.exe"
$unsigned="$output/ViVe-Desktop-$Version-Store-unsigned-x64.msix"
& $makeappx pack /d $stage /p $unsigned /o
if($LASTEXITCODE){throw 'MSIX validation/packing failed.'}
$signed="$output/ViVe-Desktop-$Version-Sideload-Test-x64.msix"
Copy-Item $unsigned $signed
$cert=New-SelfSignedCertificate -Type Custom -Subject $Publisher -KeyUsage DigitalSignature -FriendlyName 'ViVe Desktop CI sideload test only' -CertStoreLocation Cert:\CurrentUser\My -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}') -NotAfter (Get-Date).AddMonths(3)
$certificate="$output/ViVe-Desktop-Sideload-Test.cer"
Export-Certificate -Cert $cert -FilePath $certificate | Out-Null
& $signtool sign /fd SHA256 /s My /sha1 $cert.Thumbprint $signed
if($LASTEXITCODE){throw 'Test package signing failed.'}
& $signtool verify /pa $signed
# Self-signed CI certificates are deliberately not installed into a trusted root.
@"
ViVe Desktop MSIX builds
Identity: $Identity
Publisher: $Publisher

The unsigned Store package is NOT installable as-is. Before Store submission,
reserve the app and configure STORE_IDENTITY, STORE_PUBLISHER and
STORE_PUBLISHER_DISPLAY_NAME repository variables with Partner Center values.
The default identity is a TEST identity, not a reserved Store product.

The Sideload-Test MSIX is signed with an ephemeral CI test certificate. The CER
contains only its public key. Its private key is never uploaded. Windows must
trust that certificate before installation; decide whether to trust it yourself.
New CI runs use new certificates; these test packages are not a production
update channel. No certificates are installed or trusted on your PC by CI.

This app requires runFullTrust and allowElevation. Microsoft Store approval of
restricted capabilities is still required. No submission has been performed.
"@ | Set-Content "$output/README-STORE.txt"
Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($cert.Thumbprint)"
