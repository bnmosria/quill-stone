# Vellichor Theme

A warm, editorial design system for QuillStone, built on Avalonia UI.  
**Accent:** Burnt Sienna / Terracotta  
**Fonts:** Outfit (UI) · JetBrains Mono (editor) · Lora (preview)

---

## File structure

```
QuillStone/
├── Assets/Fonts/
│   ├── Outfit-Regular.ttf · -Medium.ttf · -SemiBold.ttf · -Bold.ttf
│   ├── Lora-Regular.ttf · -Medium.ttf · -SemiBold.ttf · -Italic.ttf
│   └── JetBrainsMono-Regular.ttf · -Medium.ttf · -Bold.ttf
└── Styles/Theme/
    ├── EditorTheme.axaml   ← color palettes, typography, spacing, shape, icon geometries
    ├── LightTheme.axaml    ← maps Light.* → semantic Brush.* tokens
    ├── DarkTheme.axaml     ← maps Dark.*  → semantic Brush.* tokens
    ├── ControlStyles.axaml ← styled selectors for Button, TextBox, Menu, ScrollBar, etc.
    └── ThemeManager.cs     ← runtime light/dark switcher
```

---

## How theming works

`App.axaml` loads `LightTheme.axaml` statically in `Application.Resources` so that
semantic `Brush.*` tokens are available on the very first render frame:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://QuillStone/Styles/Theme/LightTheme.axaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

`ThemeManager` is then called in `OnFrameworkInitializationCompleted` and appends the
active theme dictionary to the end of `MergedDictionaries` (highest priority), so it
correctly overrides the static fallback when toggling to dark:

```csharp
public override void OnFrameworkInitializationCompleted()
{
    ThemeManager.Initialize(this);          // appends LightTheme (or DarkTheme)
    desktop.MainWindow = new MainWindow();  // all DynamicResources resolve correctly
    base.OnFrameworkInitializationCompleted();
}
```

### Resource priority rule

Avalonia resolves `MergedDictionaries` **last-to-first** (highest index wins).
`ThemeManager` uses `Add` (not `Insert(0,…)`) so the runtime theme is always the
last entry and wins over the static fallback.

### StaticResource vs DynamicResource

Design tokens (`Radius.*`, `Font.*`, `FontSize.*`) and brush tokens (`Brush.*`) live in
`Application.Resources`, which is **not** accessible to `{StaticResource}` inside
`Application.Styles` at parse time. All references in `ControlStyles.axaml` and
`MainWindow.axaml` use `{DynamicResource}` to avoid a startup `KeyNotFoundException`.

---

## Token reference

### Backgrounds

| Token                        | Usage                    |
|------------------------------|--------------------------|
| `Brush.Background.Base`      | Window / root background |
| `Brush.Background.Surface`   | Cards, editor pane       |
| `Brush.Background.Elevated`  | Preview pane, dialogs    |
| `Brush.Background.Overlay`   | Toolbar, menu bar        |
| `Brush.Background.Sidebar`   | File tree sidebar        |
| `Brush.Background.Hover`     | Interactive hover state  |
| `Brush.Background.Active`    | Pressed / selected state |

### Text

| Token                     | Usage                           |
|---------------------------|---------------------------------|
| `Brush.Text.Primary`      | Body text, headings             |
| `Brush.Text.Secondary`    | Secondary labels, menu items    |
| `Brush.Text.Tertiary`     | Hints, timestamps, status bar   |
| `Brush.Text.Placeholder`  | Empty state, ghost text         |
| `Brush.Text.Inverse`      | Text on accent backgrounds      |

### Accent (Terracotta)

| Token                     | Usage                          |
|---------------------------|--------------------------------|
| `Brush.Accent.Primary`    | Primary buttons, active states |
| `Brush.Accent.Secondary`  | Hover on accent elements       |
| `Brush.Accent.Tertiary`   | Highlights, tag badges         |
| `Brush.Accent.Muted`      | Subtle accent backgrounds      |

### Borders

| Token                     | Usage               |
|---------------------------|---------------------|
| `Brush.Border.Default`    | Standard borders    |
| `Brush.Border.Strong`     | Emphasized borders  |
| `Brush.Border.Focus`      | Keyboard focus ring |
| `Brush.Border.Separator`  | Dividers, hairlines |

### Syntax (resolved as `Color`, consumed as `SolidColorBrush`)

`Brush.Syntax.Heading` · `Bold` · `Italic` · `Code` · `CodeBg`  
`Brush.Syntax.Link` · `Quote` · `QuoteBorder` · `Marker`

### Status

`Brush.Status.Success` · `Warning` · `Error` · `Info`

### Typography

| Token           | Value  | Usage               |
|-----------------|--------|---------------------|
| `FontSize.XS`   | 10     | Tooltips, badges    |
| `FontSize.SM`   | 12     | Menu items, labels  |
| `FontSize.Base` | 14     | Body text           |
| `FontSize.MD`   | 15     | Editor text         |
| `FontSize.LG`   | 17     | Subheadings         |
| `FontSize.XL`   | 20     | Section headings    |
| `FontSize.2XL`  | 24     | Page title          |
| `FontSize.3XL`  | 30     | Hero text           |

| Token          | Embedded font   | Fallback chain            |
|----------------|-----------------|---------------------------|
| `Font.UI`      | Outfit          | Segoe UI, sans-serif      |
| `Font.Editor`  | JetBrains Mono  | Consolas, monospace       |
| `Font.Preview` | Lora            | Georgia, serif            |

### Shape

| Token         | Value | Usage                     |
|---------------|-------|---------------------------|
| `Radius.None` | 0     | Flush/borderless elements |
| `Radius.SM`   | 3     | Toolbar buttons, badges   |
| `Radius.MD`   | 6     | Inputs, cards             |
| `Radius.LG`   | 10    | Panels                    |
| `Radius.XL`   | 14    | Modals                    |
| `Radius.Full` | 9999  | Pill shapes               |

### Toolbar icons

Defined in `EditorTheme.axaml` as `StreamGeometry` resources (Material Design paths,
24×24 viewbox). Use via `PathIcon`:

```xml
<PathIcon Data="{DynamicResource Icon.Bold}" Width="14" Height="14"/>
```

| Key                  | Action          |
|----------------------|-----------------|
| `Icon.Heading1`      | Heading 1       |
| `Icon.Heading2`      | Heading 2       |
| `Icon.Heading3`      | Heading 3       |
| `Icon.Bold`          | Bold            |
| `Icon.Italic`        | Italic          |
| `Icon.Code`          | Inline code     |
| `Icon.Link`          | Insert link     |
| `Icon.BulletList`    | Bullet list     |
| `Icon.NumberedList`  | Numbered list   |
| `Icon.Blockquote`    | Blockquote      |
| `Icon.Checkbox`      | Task checkbox   |

---

## Control classes

| Class              | Control     | Description                              |
|--------------------|-------------|------------------------------------------|
| `Primary`          | Button      | Filled accent button                     |
| `Secondary`        | Button      | Outlined button                          |
| `Ghost`            | Button      | Transparent, hover-only background       |
| `Toolbar`          | Button      | 30×30 icon button, no border, `Radius.SM`|
| `EditorInput`      | TextBox     | Bordered input with focus ring           |
| `CodeEditor`       | TextBox     | Full editor area, monospace, no adorner  |
| `EditorTabs`       | TabControl  | Editor/Preview tab strip                 |
| `EditorTab`        | TabItem     | Tab with underline active indicator      |
| `FileItem`         | ListBoxItem | File tree row                            |
| `ToolbarSeparator` | Separator   | 1×20 px vertical divider                 |
| `StatusBar`        | Border      | Bottom status strip                      |
| `Badge`            | Border      | Pill-shaped inline label                 |
| `Slim`             | ProgressBar | 2 px slim progress indicator             |

---

## Adding a token in your own controls

Always reference **semantic tokens** — never the raw `Light.*` / `Dark.*` values:

```xml
<!-- Correct — adapts to the active theme -->
<Border Background="{DynamicResource Brush.Background.Surface}">
    <TextBlock Foreground="{DynamicResource Brush.Text.Primary}"
               FontFamily="{DynamicResource Font.UI}"
               FontSize="{DynamicResource FontSize.Base}"/>
</Border>

<!-- Avoid — hardcoded to one theme -->
<Border Background="{StaticResource Light.Background.Surface}">
```

---

## Color palettes

### Light — "Vellichor Parchment"

| Role    | Value     |
|---------|-----------|
| Base    | `#F7F4EF` |
| Surface | `#FFFFFF` |
| Accent  | `#C0622A` |

### Dark — "Vellichor Ember"

| Role    | Value     |
|---------|-----------|
| Base    | `#141210` |
| Surface | `#1C1916` |
| Accent  | `#E07A3E` |
