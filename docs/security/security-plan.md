# Code Security Plan

## Before coding

- Maintain this threat model.
- Keep dependencies minimal.
- Avoid networking, telemetry, credential capture, macro recording, and raw typed-text logging.

## During implementation

- Review all P/Invoke declarations carefully.
- Keep hook callbacks fast and small.
- Ignore RunHold's own injected events.
- Release held keys on all shutdown paths.
- Store only configured virtual-key bindings and preferences.

## CI

- Enable Dependabot alerts.
- Enable CodeQL scanning.
- Run NuGet audit during restore.
- Run tests on every pull request.

## Release checks

- Run `dotnet restore`, `dotnet build`, and `dotnet test`.
- Scan release artifacts with Microsoft Defender.
- Publish checksums for direct-download packages.
- Review startup behavior and config file permissions.
- Sign builds if practical.
- Keep the privacy note current.
- Keep game compatibility warnings current.
- Run Windows App Certification Kit for any Store-targeted package.
