# User Guide

QuillStone is a desktop Markdown editor with a live preview and a project explorer sidebar.

---

## Interface overview

```
┌─────────────────────────────────────────────────────────┐
│  Title bar                                              │
├─────────────────────────────────────────────────────────┤
│  Menu bar  (File · Project · View · Help)               │
├─────────────────────────────────────────────────────────┤
│  Formatting toolbar                                     │
├──────────────┬──────────────────────────────────────────┤
│              │                                          │
│   Sidebar    │   Editor  │  Preview (split mode)        │
│  (explorer)  │                                          │
│              │                                          │
├─────────────────────────────────────────────────────────┤
│  Status bar           [ Editor | Split | Preview ]      │
└─────────────────────────────────────────────────────────┘
```

---

## File operations

| Action       | Menu            | Shortcut       |
| ------------ | --------------- | -------------- |
| New document | File → New      | `Ctrl+N`       |
| Open file    | File → Open…    | `Ctrl+O`       |
| Save         | File → Save     | `Ctrl+S`       |
| Save As      | File → Save As… | `Ctrl+Shift+S` |

- The window title shows the current filename. An asterisk (`*`) means there are unsaved changes.
- If you close the window or open/create a new file while changes are unsaved, QuillStone will ask
  whether to save, discard, or cancel.

---

## Project explorer

The sidebar on the left shows your project folder as a file tree.

**Opening a project:**

- File tree → right-click for context menu, or
- Project → Open Folder… to open an existing folder
- Project → New Project… to create a new folder-based project

**Inside the tree you can:**

- Click a file to open it in the editor
- Right-click a file or folder for: New File, New Folder, Rename, Delete
- Drag and drop files and folders to move them

**Recent projects** are listed under Project → Recent Projects. The last open project is restored
automatically on next launch.

When no project is loaded, the sidebar shows the currently open standalone file instead.

---

## Markdown preview

Three view modes are toggled from the buttons in the centre of the status bar, or from the View
menu:

| Mode    | Description                     |
| ------- | ------------------------------- |
| Editor  | Full-width editor, no preview   |
| Split   | Editor and preview side by side |
| Preview | Full-width rendered preview     |

The preview updates live as you type. It is rendered with
[Markdig](https://github.com/xoofx/markdig) and supports standard CommonMark plus tables, task
lists, and strikethrough.

**Detached preview window:** View → Full Preview opens the rendered preview in a separate, resizable
window — useful for keeping the preview on a second monitor while you edit.

---

## Formatting toolbar

The toolbar applies Markdown syntax to the selected text, or inserts a placeholder at the cursor.

| Button        | Shortcut | Inserts        |
| ------------- | -------- | -------------- |
| H1            | `Ctrl+H` | `# ` heading   |
| H2            | —        | `## ` heading  |
| H3            | —        | `### ` heading |
| Bold          | `Ctrl+B` | `**bold**`     |
| Italic        | `Ctrl+I` | `*italic*`     |
| Inline code   | —        | `` `code` ``   |
| Link          | `Ctrl+K` | `[text](url)`  |
| Bullet list   | —        | `- `           |
| Numbered list | —        | `1. `          |
| Blockquote    | —        | `> `           |
| Checkbox      | —        | `- [ ] `       |

---

## Smart list continuation

When the cursor is on a list item and you press **Enter**, QuillStone automatically inserts the next
bullet or number. Press **Enter** again on an empty list item to remove the marker and exit the
list.

---

## Theme

Toggle between the light and dark variants of the Vellichor theme via **View → Toggle Theme**
(`Ctrl+Shift+T`).

---

## Status bar

The left side of the status bar shows the cursor position, file encoding, and file type:

```
Ln 12, Col 5  ·  UTF-8  ·  Markdown
```
