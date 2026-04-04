# Theming

QuillStone has a layered theme system. Infrastructure (tokens, control styles, the switcher) is
theme-agnostic. Themes live under `Styles/Themes/` and are swapped at runtime by `ThemeManager`.
**Vellichor** is the first theme.

---

## Architecture

```
QuillStone/
├── Styles/
│   ├── Tokens.axaml                 ← shared, theme-agnostic: Font, Radius, Spacing, Shadow, Icons
│   ├── Theme/
│   │   ├── ControlStyles.axaml      ← generic control styles (Button, TextBox, Menu, …)
│   │   └── ThemeManager.cs          ← runtime light/dark switcher
│   └── Themes/
│       └── Vellichor/
│           ├── Palette.axaml        ← color values (Light.* and Dark.*)
│           ├── Light.axaml          ← maps Light.* → semantic Brush.* tokens
│           └── Dark.axaml           ← maps Dark.*  → semantic Brush.* tokens
└── Assets/
    └── Fonts/
        ├── Outfit-{Regular,Medium,SemiBold,Bold}.ttf
        ├── Lora-{Regular,Medium,SemiBold,Italic}.ttf
        └── JetBrainsMono-{Regular,Medium,Bold}.ttf
```

**Key rule:** `Tokens.axaml` and `ControlStyles.axaml` reference only `Brush.*`, `Font.*`,
`Radius.*`, etc. — never a theme-specific key like `Light.Background.Base`. This keeps them working
with any theme.

---

## How theming works

`App.axaml` loads the shared tokens and the default theme statically so all `{DynamicResource}`
bindings resolve on the first render frame:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://QuillStone/Styles/Tokens.axaml"/>
            <ResourceInclude Source="avares://QuillStone/Styles/Themes/Vellichor/Light.axaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

`ThemeManager` is then called in `OnFrameworkInitializationCompleted` and **appends** the active
theme to `MergedDictionaries` (highest priority), overriding the static fallback when toggling:

```csharp
public override void OnFrameworkInitializationCompleted()
{
    ThemeManager.Initialize(this);          // appends active theme at end of MergedDictionaries
    desktop.MainWindow = new MainWindow();  // DynamicResources resolve correctly
    base.OnFrameworkInitializationCompleted();
}
```

Avalonia resolves `MergedDictionaries` **last-to-first** — `ThemeManager` uses `Add` (not
`Insert(0,…)`) so the runtime entry always has higher priority than the static fallback.

---

## Adding a new theme

1. Create `Styles/Themes/{Name}/Palette.axaml` with your `Light.*` and `Dark.*` color values.
2. Create `Styles/Themes/{Name}/Light.axaml` and `Dark.axaml` — each includes `Palette.axaml` via
   `MergedDictionaries` then maps its colors to the semantic `Brush.*` tokens.
3. In `ThemeManager.cs`, update the two path constants:
   ```csharp
   private const string LightPath = "{Name}/Light.axaml";
   private const string DarkPath  = "{Name}/Dark.axaml";
   ```
4. Update the static fallback in `App.axaml` to point to the new `Light.axaml`.

`Tokens.axaml` and `ControlStyles.axaml` need no changes — they work with any theme that provides
the `Brush.*` contract.

---

## Token reference

### Backgrounds

| Token                       | Usage                    |
| --------------------------- | ------------------------ |
| `Brush.Background.Base`     | Window / root background |
| `Brush.Background.Surface`  | Cards, editor pane       |
| `Brush.Background.Elevated` | Preview pane, dialogs    |
| `Brush.Background.Overlay`  | Toolbar, menu bar        |
| `Brush.Background.Sidebar`  | File tree sidebar        |
| `Brush.Background.Hover`    | Interactive hover state  |
| `Brush.Background.Active`   | Pressed / selected state |

### Text

| Token                    | Usage                         |
| ------------------------ | ----------------------------- |
| `Brush.Text.Primary`     | Body text, headings           |
| `Brush.Text.Secondary`   | Secondary labels, menu items  |
| `Brush.Text.Tertiary`    | Hints, timestamps, status bar |
| `Brush.Text.Placeholder` | Empty state, ghost text       |
| `Brush.Text.Inverse`     | Text on accent backgrounds    |

### Accent

| Token                    | Usage                          |
| ------------------------ | ------------------------------ |
| `Brush.Accent.Primary`   | Primary buttons, active states |
| `Brush.Accent.Secondary` | Hover on accent elements       |
| `Brush.Accent.Tertiary`  | Highlights, tag badges         |
| `Brush.Accent.Muted`     | Subtle accent backgrounds      |

### Borders

| Token                    | Usage               |
| ------------------------ | ------------------- |
| `Brush.Border.Default`   | Standard borders    |
| `Brush.Border.Strong`    | Emphasized borders  |
| `Brush.Border.Focus`     | Keyboard focus ring |
| `Brush.Border.Separator` | Dividers, hairlines |

### Syntax

`Brush.Syntax.Heading` · `Bold` · `Italic` · `Code` · `CodeBg` `Brush.Syntax.Link` · `Quote` ·
`QuoteBorder` · `Marker`

### Status

`Brush.Status.Success` · `Warning` · `Error` · `Info`

### Typography (from `Tokens.axaml`, shared)

| Token           | Value | Usage              |
| --------------- | ----- | ------------------ |
| `FontSize.XS`   | 10    | Tooltips, badges   |
| `FontSize.SM`   | 12    | Menu items, labels |
| `FontSize.Base` | 14    | Body text          |
| `FontSize.MD`   | 15    | Editor text        |
| `FontSize.LG`   | 17    | Subheadings        |
| `FontSize.XL`   | 20    | Section headings   |
| `FontSize.2XL`  | 24    | Page title         |
| `FontSize.3XL`  | 30    | Hero text          |

| Token          | Embedded font  | Fallback chain       |
| -------------- | -------------- | -------------------- |
| `Font.UI`      | Outfit         | Segoe UI, sans-serif |
| `Font.Editor`  | JetBrains Mono | Consolas, monospace  |
| `Font.Preview` | Lora           | Georgia, serif       |

### Shape (from `Tokens.axaml`, shared)

| Token         | Value | Usage                     |
| ------------- | ----- | ------------------------- |
| `Radius.None` | 0     | Flush/borderless elements |
| `Radius.SM`   | 3     | Toolbar buttons, badges   |
| `Radius.MD`   | 6     | Inputs, cards             |
| `Radius.LG`   | 10    | Panels                    |
| `Radius.XL`   | 14    | Modals                    |
| `Radius.Full` | 9999  | Pill shapes               |

### Toolbar icons (from `Tokens.axaml`, shared)

Defined as `StreamGeometry` resources (Material Design paths, 24×24). Use via `PathIcon`:

```xml
<PathIcon Data="{DynamicResource Icon.Bold}" Width="14" Height="14"/>
```

| Key                 | Action        |
| ------------------- | ------------- |
| `Icon.Heading1`     | Heading 1     |
| `Icon.Heading2`     | Heading 2     |
| `Icon.Heading3`     | Heading 3     |
| `Icon.Bold`         | Bold          |
| `Icon.Italic`       | Italic        |
| `Icon.Code`         | Inline code   |
| `Icon.Link`         | Insert link   |
| `Icon.BulletList`   | Bullet list   |
| `Icon.NumberedList` | Numbered list |
| `Icon.Blockquote`   | Blockquote    |
| `Icon.Checkbox`     | Task checkbox |

---

## Control classes

| Class              | Control     | Description                               |
| ------------------ | ----------- | ----------------------------------------- |
| `Primary`          | Button      | Filled accent button                      |
| `Secondary`        | Button      | Outlined button                           |
| `Ghost`            | Button      | Transparent, hover-only background        |
| `Toolbar`          | Button      | 30×30 icon button, no border, `Radius.SM` |
| `EditorInput`      | TextBox     | Bordered input with focus ring            |
| `CodeEditor`       | TextBox     | Full editor area, monospace, no adorner   |
| `EditorTabs`       | TabControl  | Editor/Preview tab strip                  |
| `EditorTab`        | TabItem     | Tab with underline active indicator       |
| `FileItem`         | ListBoxItem | File tree row                             |
| `ToolbarSeparator` | Separator   | 1×20 px vertical divider                  |
| `StatusBar`        | Border      | Bottom status strip                       |
| `Badge`            | Border      | Pill-shaped inline label                  |
| `Slim`             | ProgressBar | 2 px slim progress indicator              |

---

## Using tokens in your own controls

Always reference **semantic tokens** — never raw palette values:

```xml
<!-- Correct — adapts to any theme -->
<Border Background="{DynamicResource Brush.Background.Surface}">
    <TextBlock Foreground="{DynamicResource Brush.Text.Primary}"
               FontFamily="{DynamicResource Font.UI}"
               FontSize="{DynamicResource FontSize.Base}"/>
</Border>

<!-- Avoid — hardcoded to one theme's palette -->
<Border Background="{StaticResource Light.Background.Surface}">
```

---

## Vellichor palette

### Light — "Vellichor Parchment"

| Role    | Value     |
| ------- | --------- |
| Base    | `#F7F4EF` |
| Surface | `#FFFFFF` |
| Accent  | `#C0622A` |

### Dark — "Vellichor Ember"

| Role    | Value     |
| ------- | --------- |
| Base    | `#141210` |
| Surface | `#1C1916` |
| Accent  | `#E07A3E` |
