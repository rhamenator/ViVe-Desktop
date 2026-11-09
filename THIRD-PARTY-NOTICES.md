# Third-party notices

ViVe Desktop modifications: Copyright © 2026 Richard M. Hamilton. GPL-3.0-only.
First public release: November 9, 2026. See [COPYRIGHT](COPYRIGHT) and [LICENSE](LICENSE).

## ViVe and ViVeTool

Original project by @thebookisclosed (Albacore), Copyright 2019–2025 and contributors.
Upstream: https://github.com/thebookisclosed/ViVe
Base commit: 3f8c6a3425983412da1e8b26cd757c3aa17b3f25.
Original source headers and GPL-3.0-or-later notices are preserved. The complete
corresponding modified source and build scripts accompany each release tag.

## Feature names

The dictionary includes factual ID/name mappings from ViVe and
phantomofearth/windows-velocity-feature-lists. Exact source revisions and the
separate community display labels are documented in
[Extra/DICTIONARY-SOURCES.md](Extra/DICTIONARY-SOURCES.md).

## Microsoft .NET / WPF

Self-contained Windows packages bundle unmodified Microsoft .NET 10 runtime and
WPF libraries. Their MIT and third-party terms remain applicable. Runtime license
and notice files are included under `licenses/dotnet` when present in the SDK
distribution. Source and license: https://github.com/dotnet/runtime and
https://github.com/dotnet/wpf.

## Build dependencies

Inno Setup creates the standalone installer on GitHub's Windows runner. Its
compiler is not part of the app payload; its installer engine retains the Inno
Setup license: https://github.com/jrsoftware/issrc/blob/main/LICENSE.TXT.
The original standalone CLI references Newtonsoft.Json 13.0.3 (MIT); the GUI
does not use it. Source/license: https://github.com/JamesNK/Newtonsoft.Json.
