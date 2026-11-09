# Store packaging and sideloading

The Microsoft Store package workflow uses Windows SDK MakeAppx and the same
self-contained app as the standalone installer. It does not submit to Partner Center.

Until the app is reserved, defaults are an explicit test identity. Set these
repository variables using Product identity from Partner Center:

- STORE_IDENTITY: Package/Identity/Name
- STORE_PUBLISHER: Package/Identity/Publisher (complete CN=... value)
- STORE_PUBLISHER_DISPLAY_NAME: PublisherDisplayName

The unsigned MSIX is for Store processing after these values are correct.
Microsoft signs accepted Store submissions. Changing only the display name does
not associate a package with a reserved Store product.

The app declares runFullTrust and allowElevation because it manages Windows
features as administrator. Restricted capabilities require Store approval;
successful CI packaging is not approval. Packaged/elevated registry behavior
and certification must be tested before claiming Store readiness.

## Sideload test artifact

Sideload-Test MSIX is signed with a fresh short-lived CI test certificate.
The CER is public-only. No PFX or private key is uploaded. This is not a stable
production update channel. Windows must trust the signing certificate before
installation. Review the certificate/build provenance before deciding to trust it.
Certificate trust and package installation are manual tester actions.
The workflow does not change security policy, disable antivirus or install
certificates on your PC.

References:
- https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview
- https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations
