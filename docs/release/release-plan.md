# RunHold Release Plan

RunHold supports two distribution lanes:

- Microsoft Store for the lowest-friction install and update path.
- GitHub Releases for source users and people who prefer direct downloads.

## Current repo state

- The app is a WPF desktop app.
- The GitHub release path has a publish script and release workflow that produce a self-contained `win-x64` portable ZIP with a SHA-256 checksum.
- The Store path uses MSIX. Partner Center identity values are captured in the MSIX templates, but the Windows Application Packaging Project and packaged startup behavior still need to be completed and tested.

## Auto-update policy

RunHold 1.2 uses the Microsoft Store as the only automatic update path.

- Store MSIX users receive updates through the Microsoft Store.
- Store updates must keep the same package identity and use a higher package version, such as `1.3.0.0` after `1.2.0.0`.
- GitHub portable ZIP users update manually by downloading a newer ZIP and replacing the app folder.
- Do not add Velopack, ClickOnce, an in-app update checker, a direct-download updater, or custom update network code for 1.2.
- Keep `.appinstaller` as a future direct-download MSIX option only. It is not part of the first Store release.
- If direct-download MSIX updates are added later, prefer silent background `.appinstaller` checks.

## Microsoft Store lane

Target package type: MSIX.

Expected user experience:

- User installs RunHold from the Microsoft Store.
- Microsoft Store delivers `1.2`, `1.3`, and later updates in the background.
- The app keeps the same package identity across updates, so user settings should remain available.
- Store signing is handled by Microsoft for MSIX packages submitted through Partner Center.
- RunHold does not include its own update checker or update network path.

Before Store submission:

- Confirm the RunHold Store identity in Partner Center. Current Store ID: `9P1R3CL046B8`.
- Add or update the Windows Application Packaging Project in the solution.
- Assign the Store identity from Partner Center.
- Add a packaged startup task with `Enabled="false"` so startup remains opt-in.
- Confirm whether packaged startup settings need a `Windows.ApplicationModel.StartupTask` implementation instead of the current per-user Run registry entry.
- Run the Windows App Certification Kit.
- Confirm that keyboard hooks and synthetic input pass Store policy review.

Store update process:

- Build the Store package with the same identity and a higher version.
- Upload the replacement MSIX, MSIX bundle, or MSIX upload package through Partner Center.
- Let Partner Center certification, Microsoft signing, and Microsoft Store delivery handle the update.
- Verify user settings in `%LOCALAPPDATA%\RunHold\settings.json` survive the update.

## GitHub lane

Current target: self-contained portable ZIP.

Expected user experience:

- User downloads `RunHold-{version}-win-x64-portable.zip` from GitHub Releases.
- User extracts it and runs `RunHold.exe`.
- Upgrade is manual: download the newer ZIP and replace the old folder.
- Settings remain in `%LOCALAPPDATA%\RunHold\settings.json`, so they should survive folder replacement.
- GitHub ZIP releases do not auto-update in 1.2.

Better future GitHub path:

- Add a signed installer or signed MSIX when code signing is available.
- Consider `.appinstaller` for direct-download MSIX update checks outside the Store, using silent background checks.
- Submit to WinGet after the first public release asset is stable.
- Add a GitHub Pages overview page that points to the repo and current release, using the same branding look and feel as the app.

## Versioning

- Use tags like `v1.2`.
- The publish script trims the leading `v` for assembly and artifact versions.
- Store packages use four-part package versions, such as `1.2.0.0` and `1.3.0.0`.
- Release artifacts should include both the ZIP and `.sha256.txt` checksum.

## References

- Microsoft Store and direct download overview: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app
- WPF MSIX packaging: https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net
- App Installer update settings: https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings
- Startup task packaging extension: https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-extensions
- WinGet manifest docs: https://learn.microsoft.com/en-us/windows/package-manager/package/manifest
