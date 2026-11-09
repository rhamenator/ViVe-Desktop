# Security policy

Supported: the latest 1.x release. Report suspected vulnerabilities privately with
GitHub Security → Report a vulnerability, rather than a public issue containing
an exploit or sensitive machine data.

ViVe Desktop requires administrator access and writes Windows feature overrides.
The GUI saves pre-change diagnostic snapshots locally, verifies Runtime and Boot
writes, and reports partial failures. Snapshots are not system restore points.
No change or reboot happens merely by opening the app. Restart now is an explicit
exit-dialog choice after actual session changes.

Dictionary updates are user initiated, text-only requests to public GitHub APIs
and raw content. Search text, local feature configurations and snapshots are not
uploaded. Download sizes and dictionary formats are validated before replacement.
No background updater, telemetry or network listener runs. Single-instance
coordination is scoped to the current user and Windows session.

Unsigned installer warnings, antivirus detections and Store restricted-capability
approval are separate issues. Report exact detection names and artifact SHA-256
values. Do not disable antivirus or add exclusions to install this app. CI test
certificates are not production publisher trust and their private keys are never
published.
