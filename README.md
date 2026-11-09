# ViVe Desktop

An English Windows app for inspecting and changing Windows feature configurations,
built on [ViVe](https://github.com/thebookisclosed/ViVe).

**Free software · GPL-3.0 · Windows 10/11 x64**
Copyright © 2026 Richard M. Hamilton. First public release: November 9, 2026.

[Download](https://github.com/rhamenator/ViVe-Desktop/releases/latest) ·
[Issues](https://github.com/rhamenator/ViVe-Desktop/issues) · [Security](SECURITY.md)

## Install

Download Setup-x64.exe from Releases and run it, or extract the entire portable ZIP.
.NET 10 is bundled. The installer adds Start menu and optional desktop shortcuts
and an uninstaller. The app requests administrator access when opened.
Installers are built and smoke-tested on GitHub Actions. Releases currently have
no public code-signing certificate; use the SHA-256 checksums and report exact
antivirus detections rather than disabling protection.

## Use

- Search by name or ID and combine state, priority and variant filters.
- Effective state is resolved by Windows. Expand priority details to see raw layers.
- Go to ID navigates to an exact, prefix or nearest ID without changing settings.
- Enable, Disable and Reset verify Runtime and persistent Boot overrides.
- On exit, changed sessions get one choice: **Restart now**, **Later**, or
  **Keep app open**. Opening the app alone or requesting an already-matching
  setting does not trigger that prompt.
- Update Dictionary downloads validated names only when clicked.
- Window, divider, columns, sorting and filters are remembered; Reset Layout restores defaults.
- A second launch activates the existing window. A newer version closes the old
  version normally, preserving preferences, before opening.

Windows features are experimental. Use IDs appropriate to your build.
Enabled configuration does not guarantee visible functionality. Boot snapshot
queries can show old values until reboot. Diagnostic snapshots are local exports,
not system restore points. Failed and partial writes are reported.

## Privacy

No telemetry or background updater. Dictionary updates contact GitHub; searches,
feature configurations and snapshots are not uploaded. Preferences, cache and
snapshots live under %LOCALAPPDATA%\ViVeDesktop and are retained on uninstall.

## Microsoft Store and sideloading

The app is planned to be free on the Microsoft Store after identity and restricted
capabilities are approved. It has **not** been submitted or certified.
The Store CI produces an unsigned MSIX and a signed test MSIX with a public test
certificate. Defaults use an unreserved test identity. See [Store packaging](docs/STORE.md).
A self-signed test certificate is not production publisher trust.

## Build and contribute

Requires Windows and .NET SDK 10. See [CONTRIBUTING.md](CONTRIBUTING.md).

~~~powershell
dotnet build ViVe.sln -c Release
dotnet run --project Tests/Tests.csproj -c Release
dotnet run --project LayoutTests/LayoutTests.csproj -c Release
~~~

CI builds/tests the app, builds/smoke-tests the installer, and creates MSIX packages.
Corresponding source and build scripts are available at every release tag.

## License, credit and optional donations

GPL-3.0. See [LICENSE](LICENSE), [COPYRIGHT](COPYRIGHT), and
[third-party notices](THIRD-PARTY-NOTICES.md). Original ViVe notices remain intact.
This is an independent project, not a Microsoft or Albacore product.

Donations are welcome through [Patreon](https://www.patreon.com/rhamenator), as
listed in the maintainer's other repositories. They are entirely optional:
no payment is required, and donating buys no feature, access, priority support
or other benefit. There is no quid pro quo.
