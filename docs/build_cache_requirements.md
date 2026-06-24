# Build Cache Service — Requirements for Crank Integration

## Context

Crank (the .NET benchmarking tool) has been updated to support a new `buildcache` channel that downloads pre-built runtime binaries from the Build Cache Service (BCS) instead of resolving versions from VMR/NuGet feeds. This gives per-commit granularity for performance testing and regression bisection.

The crank-side changes are complete. This document describes what's needed on the BCS/dotnet-performance-infra side to make the integration work end-to-end.

---

## Requirement 1: Public Blob Access

**Status:** Already in progress (per prior discussion).

Crank's BCS client uses unauthenticated HTTP GET requests to download artifacts. The blobs in the `pvscmdupload` storage account's `$web` container need to be publicly readable.

**URLs crank will hit:**

```
GET https://pvscmdupload.z22.web.core.windows.net/builds/{repoName}/latest/{branch}/latestBuilds.json
GET https://pvscmdupload.z22.web.core.windows.net/builds/{repoName}/buildArtifacts/{commitSha}/{configKey}/{artifactFile}
```

Where:
- `repoName` = `runtime` or `aspnetcore` (selected per-job via the `buildCacheRepo` property; defaults to the agent's `--build-cache-repo-name`)
- `branch` = e.g., `main`, `release/10.0`
- `configKey` = e.g., `coreclr_x64_linux`, `coreclr_arm64_windows` (runtime); `aspnetcore_x64_linux`, `aspnetcore_arm64_windows` (aspnetcore)
- `artifactFile` = e.g., `BuildArtifacts_linux_x64_Release_coreclr.tar.gz` (runtime); `BuildArtifacts_linux_x64_Release_aspnetcore.nupkg` (aspnetcore — the verbatim runtime-pack nupkg)

The aspnetcore `latestBuilds.json` lives at `builds/aspnetcore/latest/{branch}/latestBuilds.json` and contains only the 5 `aspnetcore_*` config keys plus an `all` entry and `branch_name`; it does **not** carry the runtime `coreclr_*` keys. Crank's parser enumerates keys dynamically, so it tolerates either repo's file.

---

## Requirement 2: Commit Index File (Not Required)

~~Originally proposed as a per-branch `commitIndex.json` mapping commits to timestamps.~~

**Decision:** Not needed. For the default case, `latestBuilds.json` provides the latest commit. For specific-commit runs (e.g., bisection), users will already know the SHAs — either from git history, GitHub, or a local tool that queries the GitHub API for the commit list. A separate index in BCS would be redundant.

If automated bisection tooling is built in the future, it can query GitHub directly for ordered commit SHAs and then check BCS blob existence per-commit.

---

## Requirement 3: latestBuilds.json Compatibility

**Resolved.** The actual `latestBuilds.json` uses PascalCase (`CommitSha`, `CommitTime`), not snake_case. Crank's parser has been updated to accept both casings for forward compatibility.

---

## Requirement 4: Artifact Layout Stability

Crank extracts **runtime** artifacts using this path convention inside the archive:

```
microsoft.netcore.app.runtime.{rid}/Release/runtimes/{rid}/lib/net{X}.0/  → managed DLLs
microsoft.netcore.app.runtime.{rid}/Release/runtimes/{rid}/native/        → native libs
{rid}.Release/corehost/                                                    → host binaries (dotnet, libhostfxr, libhostpolicy)
```

For **aspnetcore** artifacts the stored blob is the **verbatim runtime-pack nupkg** (a zip,
extension `.nupkg`), so the layout is the nupkg's own — `runtimes/{rid}` sits at the archive root,
with no `microsoft.aspnetcore.app.runtime.{rid}/Release` wrapper. Crucially, the verbatim nupkg
carries the host-resolvable framework metadata next to the managed assemblies:

```
runtimes/{rid}/lib/net{X}.0/Microsoft.AspNetCore.*.dll                  → managed assemblies
runtimes/{rid}/lib/net{X}.0/Microsoft.AspNetCore.App.deps.json          → host-resolvable metadata (REQUIRED)
runtimes/{rid}/lib/net{X}.0/Microsoft.AspNetCore.App.runtimeconfig.json → host-resolvable metadata (REQUIRED)
runtimes/{rid}/native/                                                  → native libs (optional)
```

Because the nupkg is a complete framework, crank **places `Microsoft.AspNetCore.App` directly** from
the pack into the per-job dotnet home (the whole managed set incl `deps.json`/`runtimeconfig.json`,
no feed contribution) and **fails the job** (`BuildCacheIncompleteException`) if the pack is missing
managed assemblies, `deps.json`, or `runtimeconfig.json` — for perf runs, erroring is preferable to
silently running mixed/feed bits. The base runtime + host stay feed-resolved (the aspnetcore pack
ships neither). Self-contained (SCD) publishes are the exception: the framework is co-mingled with
the app under the app's own `.deps.json`, so for SCD crank overlays only the managed `*.dll`
(+ native), not the framework metadata.

This differs from **runtime**, whose archive is raw build output (no shared-framework
`deps.json`/`runtimeconfig.json`), so the runtime flavour overlays BCS binaries onto a feed-installed
runtime (reusing the feed's metadata) rather than placing directly.

Where `{rid}` = `linux-x64`, `linux-arm64`, `win-x64`, `win-arm64`, `win-x86`. (aspnetcore v1 has no
musl/osx/arm32 configs.)

The runtime layout was confirmed by inspecting `BuildArtifacts_linux_x64_Release_coreclr.tar.gz` (no
framework metadata in the pack lib). The aspnetcore contract — a verbatim runtime-pack nupkg carrying
managed + `deps.json` + `runtimeconfig.json`, uploaded as `BuildArtifacts_{os}_{arch}_Release_aspnetcore.nupkg`
— was confirmed against a locally-built `Microsoft.AspNetCore.App.Runtime.win-x64.nupkg` and
dotnet/performance#5243's `stage-bcs-nupkg-aspnetcore.ps1`.
**If either layout changes in future builds, the crank extraction will break.** Consider treating it
as a stable contract or documenting it.

---

## Nice-to-Have: Artifact Manifest

A `manifest.json` per commit+config that describes the archive contents would make extraction more robust:

```
builds/{repoName}/buildArtifacts/{commitSha}/{configKey}/manifest.json
```

```json
{
  "runtimeVersion": "10.0.0-preview.4.26120.3",
  "commitSha": "abc123...",
  "rid": "linux-arm64",
  "managedPath": "microsoft.netcore.app.runtime.linux-arm64/Release/runtimes/linux-arm64/lib/net10.0",
  "nativePath": "microsoft.netcore.app.runtime.linux-arm64/Release/runtimes/linux-arm64/native",
  "corehostPath": "linux-arm64.Release/corehost"
}
```

This isn't blocking — crank currently discovers paths by convention — but it would decouple crank from the internal archive layout and make future changes safe.

---

## Summary

| # | Requirement | Priority | Blocking? |
|---|-------------|----------|-----------|
| 1 | Public blob access | High | Yes — crank can't download without it |
| 2 | ~~Commit index~~ | N/A | Dropped — users provide SHAs directly or use GitHub |
| 3 | `latestBuilds.json` field names | N/A | Resolved — crank parser updated to handle PascalCase |
| 4 | Artifact layout stability | Medium | Not now, but breaking changes would break crank |
| 5 | Artifact manifest.json | Low | Nice-to-have for robustness |
