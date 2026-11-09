# ViVe Desktop

An English Windows desktop interface built directly on the upstream ViVe library.
Upstream: https://github.com/thebookisclosed/ViVe
Based on commit 3f8c6a3425983412da1e8b26cd757c3aa17b3f25.
This is an independent modification, not an official upstream release. GPL-3.0-or-later; see LICENSE.

## Run

Open ViVe.Desktop.exe from the portable release folder. Keep the entire folder together.
The x64 GUI includes .NET 10 and needs no separate runtime installation.
The executable requires administrator access at launch. Desktop and Start menu shortcuts also request elevation. Accept the Windows UAC prompt; elevation lasts until the application process exits.

Search by ID or internal name, select a row or enter a positive ID, and query, enable,
disable, or reset its User-priority override in BOTH Runtime and Boot. The store selector chooses what to inspect, not where writes go.
Reset removes only the User override in both stores. Higher-priority overrides
can mask a User change. Internal names come from the bundled upstream dictionary and
may be unknown or outdated on newer Windows builds.

Changes require an in-app confirmation and a successful pre-change JSON snapshot.
Snapshots are stored in %LOCALAPPDATA%\ViVeDesktop\Snapshots and are diagnostic exports,
not automatic restore points. They are never uploaded by this application.
Boot writes also attempt to mark BootPending, and failures are reported separately.
Experimental feature changes can destabilize Windows; consult documentation for the
specific feature ID before changing it. Restart Windows manually when necessary.

## Privacy and scope

The GUI references only ViVe and Microsoft desktop libraries. It connects to GitHub only when you click Update Dictionary. It has no telemetry, web view, background updater, or startup service.
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

## Feature dictionary update (1.0.3)

Added missing symbolic names from a pinned newer feature dump, preserving all
existing mappings. IDs 61934903 and 60813048 have explicitly marked community
display labels because no internal symbolic names were available in that dump.
See Extra/DICTIONARY-SOURCES.md for provenance and limitations.


## Dictionary updates and browsing (1.1.0)

- Update Dictionary downloads a text-only feature dump from the public
  phantomofearth/windows-velocity-feature-lists GitHub repository. It pins reads to
  one commit and uses the highest versioned rs_prerelease/amd64 list.
- Only validated symbolic name/ID pairs are merged. Existing entries remain unless
  the download provides a symbolic name for that ID. Community labels remain
  explicitly marked until a symbolic name replaces them.
- No downloads run on startup. GitHub sees the normal request metadata, including
  your IP address; no feature configurations, snapshots, or search text are sent.
- Updates are saved to %LOCALAPPDATA%\ViVeDesktop\Dictionary\FeatureDictionary.pfs.
  The previous saved dictionary is retained as .bak; invalid or failed downloads
  keep existing names. GitHub rate limits or network failures can be retried later.
- The top field filters the configured rows by name or ID substring. State,
  priority and exact variant filters combine. Clear filters restores the full list.
  Column headings sort the list, including priority and variant. Payload is visible.
- Go to ID is navigation: exact match first, then prefix and nearest numeric ID.
  It clears list filters so its result is visible. Press Enter in its ID field to
  navigate. Selection fills the ID field used by Enable, Disable and Reset.
- State is per configuration row; multiple priority rows may exist for one ID.
  A Disabled row is not proof that the effective feature is disabled if another
  priority overrides it. Variant 0 is a numeric variant, not the disabled state.
- The new original icon source is Assets/App.svg; multi-resolution App.ico is
  embedded in the executable and used by the window and shortcuts.

Additional validation:
    dotnet run --project Tests/Tests.csproj -c Release -- --download

This tests filtering, merge rejection, preservation of community labels, local
cache reload/backup, and a live text-only download. No Windows feature writes occur.

## Effective feature states (1.2.0)

The Effective state and Resolved priority columns use Windows'
RtlQueryFeatureConfiguration result once per distinct ID in the selected store.
They do not guess by sorting priority rows. The original Row state, Row priority,
Row variant, and payload remain visible for inspection.

State filtering defaults to Effective state. Switch State applies to to Individual
row to filter raw override rows. Row priority and Row variant remain row filters.
All rows for an ID show the same resolved state for the selected store.
Refresh features, switching stores, Go to ID and successful writes refresh the
relevant resolved values. Results are snapshots, not live monitoring.

Default means the consuming application's built-in default is not exposed by this
API; it is not treated as Disabled. Unknown means the query failed or returned
invalid data; its tooltip includes diagnostic details. Boot-store results do not
describe the current Runtime store. Enabled configuration does not guarantee the
corresponding UI is available: Windows may have additional dependencies.

Textbox guide:
- Search name or ID (contains): filters the visible list by text or partial ID.
- Row variant (exact number): narrows those results to a specific variant value.
- Feature ID beside Go to ID: navigates to an exact/prefix/closest ID and selects
  the target for change actions. It clears list filters so the target is visible.


## Persistent changes, elevation and restart notice (1.3.0)

- The GUI manifest now requires administrator access. Its startup guard rejects
  non-elevated direct DLL launches. The elevation button is removed.
- Desktop and Start menu shortcuts have the RunAsUser flag. The elevated GUI is
  the main app process, rather than a temporary privileged helper. UAC remains
  under Windows' control; canceling elevation prevents the GUI from starting.
- Enable, Disable and Reset now match the CLI default: Runtime first, then Boot,
  followed by BootPending status. The browse-store selector never limits writes.
  This fixes the old default that wrote Runtime only, which could be lost at restart.
- Pre-change diagnostic snapshots capture BOTH stores before any write. Runtime
  changes and the persistent User-priority registry override are read back after
  writes; a failure is reported rather than falsely saying everything succeeded.
  The operation is not transactional: Runtime can succeed while Boot fails, and
  this partial outcome is explicitly reported.
- Success displays a Restart required message box with the exclamation icon and
  a persistent reminder. Reboot Windows manually; the application never restarts
  the computer automatically. A saved/enabled configuration is not proof that the
  associated Windows interface has activated before reboot or that all feature
  dependencies are met.
- The default table shows one resolved row per ID. Show individual priority rows
  (advanced) reveals the underlying layers. An Enabled User layer and Disabled
  ImageDefault layer for one ID are expected, not duplicate failed changes.
- Installation in the system drive root is not required by our implementation:
  it calls the same ViVe native/registry APIs directly, independent of working directory.

Verification includes read-only Windows queries and injected write-service tests.
The latter check both-store ordering and failure handling without changing the
host's feature configuration. Reboot-dependent behavior still requires a manual
post-reboot check on the target Windows build.

## Compact adaptive layout and single instance (1.4.0)

The search field and all five filter dropdowns share an adaptive row at normal
width. Search starts compact and receives extra width as the window grows.
Filter controls share spare width up to readable maximum sizes. Narrow windows
wrap whole labeled controls without overlap.

The initial window is shorter, with a moderate table height and a visible activity
log. Drag the horizontal divider above Activity log to adjust the table/log split.
The table and log grow with the window. Explanations use tooltips and the action
row is compact. Administrator launch, both-store writes, snapshots, effective
states and the reboot message remain unchanged.

Only one GUI instance runs per user in a Windows session. Launching it again
requests activation of the existing window, restoring it if minimized, and exits
the second process. No feature configuration is changed by activation.

The installed update uses a versioned folder so a running elevated older copy
cannot block file replacement. Desktop and Start menu shortcuts target the new
executable and keep their administrator flags.

Layout verification at 980x720, 1300x800 and 1800x1100 checks control boundaries,
overlap, wrapping and growth. The normal-size table is about 296 pixels tall;
the splitter lets the user choose a different proportion.

To repeat the noninteractive WPF layout checks:
    dotnet run --project LayoutTests/LayoutTests.csproj -c Release

The layout checks measure/arrange the window content without showing the window
or writing Windows features. Single-instance startup was also checked by launching
the installed elevated executable twice and confirming only the first PID remains.

## Remembered layout and taller log (1.5.0)

Window size, position, maximized state and the table/log split are saved on normal
close to %LOCALAPPDATA%\ViVeDesktop\window-layout.json and restored on launch.
Normal restore bounds are kept while maximized or minimized. Closing minimized
does not make the next launch start minimized.

Bounds are stored in native screen pixels and clamped to a current monitor's work
area if a display is removed or resolution changes. Invalid, missing or corrupt
settings fall back to defaults. Settings are local and are not uploaded.

The default table/log allocation is now 3:1 instead of 4:1, and the log has a
100-pixel minimum instead of 70. Saved proportions override these defaults.
Drag the divider to choose your own balance; it is remembered on close.

Layout tests now include actual window close/reopen, position and size round trips,
maximized and minimized-state handling, divider persistence, corrupt-file fallback
and disconnected-monitor recovery. Test windows use temporary settings files.

## Remembered table/filter preferences and version handoff (1.6.0)

Column widths (including pixel/star sizing), all sort descriptions, search text,
browse store, state scope/state, row priority, variant and priority-detail mode
are saved on close to %LOCALAPPDATA%\ViVeDesktop\view-preferences.json.
Sorting is reapplied after filtering, refreshing, changing stores or updating names.
Column identities use binding property names, rather than display labels.

Preferences are local only. Missing or corrupt settings fall back to defaults;
unknown column/sort keys are ignored. Layout settings remain separate.

The activity log gets about 36 additional pixels on the first upgrade from the
older layout format, and its default minimum is 140 pixels. The updated layout
format prevents that adjustment repeating on later launches. Afterward the
user's saved divider position is respected.

Version behavior:
- Same or older version: activate the running window and exit the duplicate.
- Newer version: request normal close of the older same-user/session product,
  wait for it to save settings and release the single-instance lock, then open.
- If an older window is busy or refuses to close, show an explanatory warning
  and leave it running. It is never force-terminated by the app.
- Closing is deferred while the current GUI performs an operation.

Verified against the v1.5 window left open by the user: a duplicate v1.5 launch
retained its original PID; v1.6 then closed v1.5 normally and opened; duplicate v1.6
and older v1.5 launches both retained the v1.6 PID with only one GUI instance.
Preference tests cover restoration, multicolumn sort, pixel/star widths,
filtering without losing sort, close/save and corrupt-file fallback.
