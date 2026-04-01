# Quill-Stone

Quill-Stone is a lightweight desktop Markdown editor built with C# and WPF (.NET 8). It lets you create, open, edit, and save `.md` files with a clean, distraction-free interface and standard OS file dialogs.

## Features (Phase 1)

- **New / Open / Save / Save As** — full file workflow with standard OS dialogs, filtered to `.md` by default
- **Unsaved-changes protection** — prompts to Save / Don't Save / Cancel when closing, or before New/Open discards edits
- **Window title reflects state** — `Untitled - Quill-Stone` for new docs, `filename.md - Quill-Stone` when saved, `filename.md* - Quill-Stone` when dirty
- **Keyboard shortcuts** — `Ctrl+N` New, `Ctrl+O` Open, `Ctrl+S` Save, `Ctrl+Shift+S` Save As
- **Editor** — monospaced multiline TextBox with scroll bars, undo/redo, copy/paste

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)
- Windows 10 or later (WPF is Windows-only)

## Build & Run

```bash
cd QuillStone
dotnet build
dotnet run
```

Or open `QuillStone/QuillStone.csproj` in Visual Studio 2022+ and press **F5**.
