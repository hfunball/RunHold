# RunHold Release Plan

RunHold supports two distribution lanes:

- Microsoft Store for the lowest-friction install and update path.
- GitHub Releases for source users and people who prefer direct downloads.

## Current repo state

- The app is a WPF desktop app.
- The GitHub release path has a publish script and release workflow that produce a self-contained `win-x64` portable ZIP with a SHA-256 checksum.
- The Store path uses MSIX. Partner Center identity values are captured in the MSIX templates, but the Windows Application Packaging Project and packaged startup behavior still need to be completed and tested.

## Microsoft Store lane

Target package type: MSIX.

Expected user experience:

- User installs RunHold from the Microsoft Store.
- Microsoft Store delivers `1.2`, `1.3`, and later updates.
- The app keeps the same package identity across updates, so user settings should remain available.
- Store signing is handled by Microsoft for MSIX packages submitted through Partner Center.

Before Store submission:

- Confirm the RunHold Store identity in Partner Center. Current Store ID: `9P1R3CL046B8`.
- Add or update the Windows Application Packaging Project in the solution.
- Assign the Store identity from Partner Center.
- Add a packaged startup task with `Enabled="false"` so startup remains opt-in.
- Confirm whether packaged startup settings need a `Windows.ApplicationModel.StartupTask` implementation instead of the current per-user Run registry entry.
- Run the Windows App Certification Kit.
- Confirm that keyboard hooks and synthetic input pass Store policy review.

## GitHub lane

Current target: self-contained portable ZIP.

Expected user experience:

- User downloads `RunHold-{version}-win-x64-portable.zip` from GitHub Releases.
- User extracts it and runs `RunHold.exe`.
- Upgrade is manual: download the newer ZIP and replace the old folder.
- Settings remain in `%LOCALAPPDATA%\RunHold\settings.json`, so they should survive folder replacement.

Better future GitHub path:

- Add a signed installer or signed MSIX when code signing is available.
- Consider `.appinstaller` for MSIX auto-update checks outside the Store.
- Submit to WinGet after the first public release asset is stable.
- Add a GitHub Pages overview page that points to the repo and current release, using the same branding look and feel as the app.

## Versioning

- Use tags like `v1.2`.
- The publish script trims the leading `v` for assembly and artifact versions.
- Release artifacts should include both the ZIP and `.sha256.txt` checksum.

## References

- Microsoft Store and direct download overview: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app
- WPF MSIX packaging: https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net
- App Installer update settings: https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings
- Startup task packaging extension: https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-extensions
- WinGet manifest docs: https://learn.microsoft.com/en-us/windows/package-manager/package/manifest
