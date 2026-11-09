# ViVe Desktop

An English, offline Windows desktop interface built directly on the upstream ViVe library.
Upstream: https://github.com/thebookisclosed/ViVe
Based on commit 3f8c6a3425983412da1e8b26cd757c3aa17b3f25.
This is an independent modification, not an official upstream release. GPL-3.0-or-later; see LICENSE.

## Run

Open ViVe.Desktop.exe from the portable release folder. Keep the entire folder together.
The x64 GUI includes .NET 10 and needs no separate runtime installation.
It starts read-only. Run as administrator requests Windows elevation when you choose it.

Search by ID or internal name, select a row or enter a positive ID, and query, enable,
disable, or reset its User-priority override. Choose Runtime or Boot explicitly.
Reset removes only the User override in the selected store. Higher-priority overrides
can mask a User change. Internal names come from the bundled upstream dictionary and
may be unknown or outdated on newer Windows builds.

Changes require an in-app confirmation and a successful pre-change JSON snapshot.
Snapshots are stored in %LOCALAPPDATA%\ViVeDesktop\Snapshots and are diagnostic exports,
not automatic restore points. They are never uploaded by this application.
Boot writes also attempt to mark BootPending, and failures are reported separately.
Experimental feature changes can destabilize Windows; consult documentation for the
specific feature ID before changing it. Restart Windows manually when necessary.

## Privacy and scope

The GUI references only ViVe and Microsoft desktop libraries. It has no HTTP client,
telemetry, web view, updater, startup service, or online feature dictionary download.
The original CLI remains separate and retains its explicit upstream update commands.
This is source-based transparency, not a certification or a comprehensive security audit.
The GUI covers common state operations; advanced variants, subscriptions, priority
migration, and LKG repair remain CLI/library features.

## Build and verify

Requires Windows and the .NET 10 SDK. The CLI additionally requires .NET Framework
4.8.1 at runtime; SDK restore provides build references.

    dotnet build ViVe.sln -c Release
    dotnet run --project Tests/Tests.csproj -c Release
    dotnet publish ViVe.Desktop/ViVe.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts/desktop

The tests check native layouts, packed fields, bounds, immutable priority and invalid
policy guards, and read-only native queries of both stores. They never write Windows
features. GUI startup and search were also verified on Windows build 26300.
Elevated feature writes and LKG repair were not executed against the host.

## Local fixes

- Corrected LKG registry path to SYSTEM\CurrentControlSet and opened the key writable.
- Guarded missing, short and misaligned LKG blobs before repair/copy.
- Opened policy reset keys writable and made already-absent values idempotent.
- Migrated projects to SDK-style restore; library targets net481 and net10.0-windows.
  This removes reliance on an un-restored local packages directory while preserving CLI.
- Added the WPF GUI, read-only default, elevation, confirmation, snapshots and error log.

The upstream commit history and original license are preserved.

## Query ID update (1.0.1)

Successful queries now select the matching row and explicitly say "Found feature",
with state and priority in readable text. "Name unavailable" means the offline
dictionary lacks a name, not that Windows lacks the ID.
Unsuccessful single queries fall back to enumeration, check the other store, and
show native status codes and dictionary knowledge separately.
Regression checks query every enumerated ID in both stores without modifying features.

Query ID also navigates to the closest configured ID when no exact configuration
exists in the selected store: prefix matches first, then numeric distance; ties use
the lower ID. The selected full ID is shown in the ID field and log. No feature is
changed by navigation.
