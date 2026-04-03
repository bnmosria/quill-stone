# QuillStone

QuillStone is a lightweight, cross-platform desktop Markdown editor built with C#
and [Avalonia UI](https://avaloniaui.net/) (.NET 10). It runs natively on **Windows 11** and **macOS** and lets you
create, open, edit, and save `.md` files with a clean, distraction-free interface.

## Features

### File operations

- **New / Open / Save / Save As** — full file workflow with native OS dialogs, filtered to `.md` by default
- **Unsaved-changes protection** — prompts to Save / Don't Save / Cancel when closing the window, or before New/Open
  would discard edits
- **Window title reflects state** — `Untitled - QuillStone` for new docs, `filename.md - QuillStone` when saved,
  `filename.md* - QuillStone` when there are unsaved changes

### Editor

- Monospaced multiline text area with horizontal and vertical scroll bars
- Undo/redo and copy/paste work as expected
- Smart list continuation — pressing Enter on a list item automatically inserts the next bullet or number; pressing
  Enter on an empty list item removes the marker

### Formatting toolbar

| Button       | Action                      |
|--------------|-----------------------------|
| **B**        | Bold (`**…**`)              |
| *I*          | Italic (`*…*`)              |
| `` ` ` ``    | Inline code (`` `…` ``)     |
| 🔗           | Insert link (`[text](url)`) |
| H1 / H2 / H3 | Heading levels 1–3          |
| • List       | Bullet list (`- `)          |
| 1. List      | Numbered list (`1. `)       |
| ❝            | Blockquote (`> `)           |
| ☑            | Task checkbox (`- [ ] `)    |

### Keyboard shortcuts

| Shortcut       | Action       |
|----------------|--------------|
| `Ctrl+N`       | New document |
| `Ctrl+O`       | Open file    |
| `Ctrl+S`       | Save         |
| `Ctrl+Shift+S` | Save As      |
| `Ctrl+B`       | Bold         |
| `Ctrl+I`       | Italic       |
| `Ctrl+K`       | Insert link  |
| `Ctrl+H`       | Heading 1    |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 11 or macOS (Intel and Apple Silicon)

## Build, Run, and Publish

There is no `.sln` file in this repo. Build/publish directly from `QuillStone/QuillStone.csproj`.

- `dotnet build` compiles the project for development (fast local builds).
- `dotnet publish` creates deployable artifacts for a target runtime.

### Build and run (dev)

```bash
dotnet build QuillStone/QuillStone.csproj
dotnet run --project QuillStone/QuillStone.csproj
```

### Publish self-contained executables (Release)

```bash
# macOS Apple Silicon
dotnet publish QuillStone/QuillStone.csproj -c Release -r osx-arm64 --self-contained true

# macOS Intel (optional)
dotnet publish QuillStone/QuillStone.csproj -c Release -r osx-x64 --self-contained true

# Windows x64
dotnet publish QuillStone/QuillStone.csproj -c Release -r win-x64 --self-contained true
```

Typical output folders:

- `QuillStone/bin/Release/net10.0/osx-arm64/publish/`
- `QuillStone/bin/Release/net10.0/osx-x64/publish/`
- `QuillStone/bin/Release/net10.0/win-x64/publish/`

Main app artifact names:

- macOS: `QuillStone`
- Windows: `QuillStone.exe`

## Platform notes

QuillStone uses Avalonia UI instead of WPF, so it runs without modification on both Windows and macOS. There are no
Windows-only dependencies; native file open/save dialogs and storage APIs are provided by Avalonia's cross-platform
`StorageProvider`.

## Theme

QuillStone ships the **Vellichor** theme — a warm, editorial design language with light and dark
variants, embedded fonts (Outfit, JetBrains Mono, Lora), and a full semantic token set. The theme
infrastructure is designed to support multiple themes; Vellichor is the first.

See [docs/THEME.md](docs/THEME.md) for the token reference, control classes, and guide for adding new themes.
