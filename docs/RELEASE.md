# Release & Deployment

This document covers local packaging and the GitHub Actions CI/CD pipeline.

---

## Local packaging

### macOS — `.app` bundle

Use `package-mac.sh` from the repo root. Requires macOS and the .NET 10 SDK.

```bash
./package-mac.sh           # Intel x64 (default)
./package-mac.sh arm64     # Apple Silicon
```

Output: `QuillStone.app` in the repo root. Double-click to run, or drag to `/Applications`.

The script:

1. Publishes a self-contained binary via `dotnet publish`
2. Assembles the `.app` bundle structure (`Contents/MacOS`, `Contents/Resources`)
3. Writes `Info.plist` with bundle metadata
4. Embeds `icon.icns`

### Windows — MSI installer

#### On Windows (PowerShell) — produces an `.msi`

```powershell
.\package-win.ps1           # x64 (default)
.\package-win.ps1 -Arch arm64
```

Output: `QuillStone-win-x64.msi` (or `arm64`). Double-click to install. Includes a desktop shortcut
and an entry in Add/Remove Programs.

The script auto-installs [WiX Toolset v4](https://wixtoolset.org/) as a dotnet global tool if it is
not already present, then harvests all published files and builds the MSI.

> **Important:** The `$UpgradeCode` GUID in `package-win.ps1` must never change. Windows uses it to
> recognise that a new install is an upgrade of the same product. If you change it, old installs
> will not be replaced and two entries will appear in Add/Remove Programs.

#### On macOS — produces a `.zip` for transfer

```bash
./package-win.sh           # x64 (default)
./package-win.sh arm64
```

Output: `QuillStone-win-x64.zip`. Transfer to a Windows machine, extract, and run `QuillStone.exe`.
No installer.

---

## CI/CD — GitHub Actions

Two workflows live in `.github/workflows/`.

### `ci.yml` — Continuous integration

**Trigger:** every push to `main` and every pull request targeting `main`.

**What it does:** builds the project for all three platform targets in parallel to catch compilation
failures early.

| Runner                         | Runtime ID  |
| ------------------------------ | ----------- |
| `macos-latest` (Apple Silicon) | `osx-arm64` |
| `macos-13` (Intel)             | `osx-x64`   |
| `windows-latest`               | `win-x64`   |

No artifacts are uploaded — CI is a build gate only.

### `release.yml` — Release pipeline

**Trigger:** pushing a version tag, e.g. `v1.2.0`.

```bash
git tag v1.2.0
git push origin v1.2.0
```

**What it does:**

1. Builds and packages for all four targets in parallel:
   - `osx-arm64` → `QuillStone-osx-arm64.zip` (`.app` bundle zipped)
   - `osx-x64` → `QuillStone-osx-x64.zip`
   - `win-x64` → `QuillStone-win-x64.msi`
   - `win-arm64` → `QuillStone-win-arm64.msi`
2. Creates a GitHub Release named `QuillStone <tag>` with auto-generated release notes from commit
   messages.
3. Attaches all four artifacts to the release.

**Required permission:** the workflow uses `permissions: contents: write` to create the release. For
private repos, make sure **Actions** are enabled and that the default `GITHUB_TOKEN` has write
access — check this under Settings → Actions → General → Workflow permissions → select "Read and
write permissions".

---

## Versioning

The version number is set in two places — keep them in sync when cutting a release:

| File                           | Field         |
| ------------------------------ | ------------- |
| `QuillStone/QuillStone.csproj` | `<Version>`   |
| `package-mac.sh`               | `APP_VERSION` |
| `package-win.ps1`              | `$AppVersion` |

The Git tag (`v1.2.0`) drives the GitHub Release name. The `<Version>` in the `.csproj` drives the
MSI product version and the `CFBundleVersion` in `Info.plist`.

---

## Dependencies

| Tool           | Required for     | Install                                                                   |
| -------------- | ---------------- | ------------------------------------------------------------------------- |
| .NET 10 SDK    | All builds       | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) |
| WiX Toolset v4 | Windows MSI      | `dotnet tool install --global wix` (auto-installed by `package-win.ps1`)  |
| Git            | Tagging releases | pre-installed                                                             |
