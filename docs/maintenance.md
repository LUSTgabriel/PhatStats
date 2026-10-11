# Maintenance and releases

## Local verification

1. `python tools/version.py --check` and `python tools/audit_release.py` from repository root.
2. `cd firmware`: `python tools/audit_project.py`; `pio run` (Core 6.2.0). Never flash as part of CI.
3. Windows + .NET 10 SDK: `cd host`; `./build_single_exe.ps1`. This runs frozen-source checks, compile and existing behavioral self-tests, then publishes a self-contained win-x64 bundle.
4. `python tools/collect_sources.py`, then `python tools/package_release.py` at root. Packages compiled output, four installation guides, license texts/notices, dependency inventory and matching source. Verify ZIP/checksums before release.

Release files are generated under `host/dist/` and must not be committed to Git. GitHub Actions performs audits/builds for pushes and PRs. A tag beginning `v` runs the same builds and creates a **prerelease** only after both components pass; tags must match `VERSION.json`. Do not move a published release tag to another commit.

## Version and dependency updates

Edit `VERSION.json` first, run `python tools/version.py` to synchronize MSBuild and firmware metadata, then update changelog/install docs and run verification. Display `0.01alpha` is intentionally distinct from .NET-compatible `0.0.1-alpha`; Git tag is `v0.01alpha`.

Host direct dependencies are exact versions in the project; transitive packages are locked in `packages.lock.json`. To intentionally update dependencies, change references and run restore with `--force-evaluate`, review the lock diff and third-party notices/source links, then test. PlatformIO libraries are pinned to commits. SDK is .NET 10.0.401 with latestPatch roll-forward and the bundled runtime is pinned to 10.0.11: build records include the actual SDK and dependency inventory; byte-for-byte reproducibility across SDK versions is not claimed.

## Replacing a library

All host application source and build scripts are provided. To relink with modified dependencies, build the dependency from its corresponding source, publish it to a local NuGet feed with the referenced version or intentionally change the PackageReference version, add the local feed with `dotnet nuget add source`, restore with `--force-evaluate`, and rebuild. No signature check prevents running a rebuilt host. Review LGPL/MPL obligations before distributing changed dependencies. Firmware libraries can be substituted through `platformio.ini` and rebuilt from full application source.

Never bundle proprietary installers just for convenience. PawnIO and USB drivers stay official links unless their specific redistribution terms are checked again. Keep third-party license texts and corresponding-source archives with any binary release.
