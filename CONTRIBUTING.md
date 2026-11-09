# Contributing

Use a focused issue or pull request. Include the Windows build, app version,
reproduction steps and relevant redacted log lines. Do not publish full local
snapshots or personal filesystem paths unless you intend them to be public.

Create a branch, make a focused change, and run the checks below on Windows:

```powershell
dotnet build ViVe.sln -c Release
dotnet run --project Tests/Tests.csproj -c Release
dotnet run --project LayoutTests/LayoutTests.csproj -c Release
```

Tests must not change the runner's Windows feature configuration or reboot it.
Use injected write functions for failure-path tests. Describe any authorized
manual feature tests explicitly, including what cannot be verified before reboot.

Contributions to ViVe Desktop are GPL-3.0-only; preserve existing upstream notices.
Sign off each new commit with `git commit -s` under the
[Developer Certificate of Origin 1.1](https://developercertificate.org/).
Do not claim independent review for maintainer self-review. The solo maintainer
may merge their own PR after required checks and conversations are resolved.

Required checks protect the default branch. Do not force-push default/release
history. Packaging and release changes must include CI evidence and update the
changelog. Store uploads require the actual Partner Center identity and capability
approval; test MSIX files must remain clearly labeled.
