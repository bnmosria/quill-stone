# QuillStone

A lightweight, cross-platform desktop Markdown editor built with C# and
[Avalonia UI](https://avaloniaui.net/) (.NET 10). Runs natively on **Windows** and **macOS** (Intel
and Apple Silicon).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11 or macOS
- Node.js (for Prettier)

## Build and run

```bash
dotnet build QuillStone/QuillStone.csproj
dotnet run --project QuillStone/QuillStone.csproj
```

## Code formatting

**C#** — uses `dotnet format` with rules defined in `.editorconfig`:

```bash
dotnet format QuillStone/QuillStone.csproj
```

**Docs** (Markdown, JSON, YAML) — uses [Prettier](https://prettier.io/) with rules defined in
`.prettierrc`:

```bash
npx prettier --write .
```

Both are enforced in CI on every pull request.

## Documentation

- [docs/USER_GUIDE.md](docs/USER_GUIDE.md) — editor features, project explorer, preview, shortcuts
- [docs/THEME.md](docs/THEME.md) — theme token reference and guide for adding new themes
- [docs/RELEASE.md](docs/RELEASE.md) — packaging scripts and release pipeline

## Platform notes

Uses Avalonia UI instead of WPF — no Windows-only dependencies. Native file dialogs are provided by
Avalonia's cross-platform `StorageProvider`.
