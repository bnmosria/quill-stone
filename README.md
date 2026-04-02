# QuillStone

QuillStone is a lightweight desktop Markdown editor built with C# and Avalonia (.NET 10). It lets you create, open, edit, and save `.md` files with a clean, distraction-free interface and native file dialogs.

## Features (Phase 1)

- **New / Open / Save / Save As** — full file workflow with standard OS dialogs, filtered to `.md` by default
- **Unsaved-changes protection** — prompts to Save / Don't Save / Cancel when closing, or before New/Open discards edits
- **Window title reflects state** — `Untitled - QuillStone` for new docs, `filename.md - QuillStone` when saved, `filename.md* - QuillStone` when dirty
- **Keyboard shortcuts** — `Ctrl+N` New, `Ctrl+O` Open, `Ctrl+S` Save, `Ctrl+Shift+S` Save As
- **Formatting shortcuts** — `Ctrl+B` Bold, `Ctrl+I` Italic, `Ctrl+K` Insert Link, `Ctrl+H` Heading 1
- **Editor** — monospaced multiline TextBox with scroll bars, undo/redo, copy/paste

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- macOS or Windows

## Build & Run

```bash
cd QuillStone
dotnet build
dotnet run
```
