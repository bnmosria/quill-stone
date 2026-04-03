# Vellichor – Markdown Editor Theme

A warm, editorial design system for Avalonia MD editors.  
**Accent:** Burnt Sienna / Terracotta  
**Fonts:** Outfit (UI) · JetBrains Mono (editor) · Lora (preview)

---

## File Structure

```
Themes/
├── EditorTheme.axaml      ← All color tokens + typography + spacing (both palettes)
├── LightTheme.axaml       ← Maps Light.* tokens → semantic Brush.* tokens
├── DarkTheme.axaml        ← Maps Dark.* tokens  → semantic Brush.* tokens
├── ControlStyles.axaml    ← Style selectors for Button, TextBox, TabItem, etc.
└── ThemeManager.cs        ← Runtime theme switcher

Views/
└── MainWindow.axaml       ← Full editor layout using semantic Brush.* tokens
```

---

## Quick Start

### 1. Add fonts to your project

Place these fonts in `Assets/Fonts/`:

- **Outfit** (UI chrome) – https://fonts.google.com/specimen/Outfit
- **JetBrains Mono** (editor) – https://www.jetbrains.com/legalnotices/monofont
- **Lora** (preview) – https://fonts.google.com/specimen/Lora

Or swap them out in `EditorTheme.axaml` → `Font.UI`, `Font.Editor`, `Font.Preview`.

### 2. Bootstrap in App.axaml.cs

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MarkdownEditor.Themes;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Detect OS preference or load from settings
        var prefersDark = Environment.GetEnvironmentVariable("SYSTEM_THEME") == "dark";
        ThemeManager.Initialize(this, prefersDark ? ThemeVariant.Dark : ThemeVariant.Light);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new Views.MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
```

### 3. Toggle at runtime

Wire the theme button in MainWindow.axaml.cs:

```csharp
private void ThemeToggleButton_Click(object? sender, RoutedEventArgs e)
    => ThemeManager.Toggle();
```

Or from a ViewModel:

```csharp
ThemeManager.ThemeChanged += (_, variant) =>
    IsDark = variant == ThemeVariant.Dark;
```

### 4. Using tokens in your own controls

Always reference **semantic tokens** (never the Light/Dark-prefixed ones directly):

```xml
<!-- ✅ Correct – adapts to theme -->
<Border Background="{DynamicResource Brush.Background.Surface}">
    <TextBlock Foreground="{DynamicResource Brush.Text.Primary}"/>

    <!-- ❌ Avoid – hardcoded to one theme -->
    <Border Background="{StaticResource Light.Background.Surface}">
```

---

## Token Reference

### Backgrounds

| Token                       | Usage                    |
|-----------------------------|--------------------------|
| `Brush.Background.Base`     | Window / root background |
| `Brush.Background.Surface`  | Cards, editor pane       |
| `Brush.Background.Elevated` | Preview pane, dialogs    |
| `Brush.Background.Overlay`  | Toolbar, overlay panels  |
| `Brush.Background.Sidebar`  | File tree sidebar        |
| `Brush.Background.Hover`    | Interactive hover state  |
| `Brush.Background.Active`   | Pressed / selected state |

### Text

| Token                    | Usage                           |
|--------------------------|---------------------------------|
| `Brush.Text.Primary`     | Body text, headings             |
| `Brush.Text.Secondary`   | Secondary labels, menu items    |
| `Brush.Text.Tertiary`    | Hints, line numbers, timestamps |
| `Brush.Text.Placeholder` | Empty state, ghost text         |
| `Brush.Text.Inverse`     | Text on accent backgrounds      |

### Accent (Terracotta)

| Token                    | Usage                          |
|--------------------------|--------------------------------|
| `Brush.Accent.Primary`   | Primary buttons, active states |
| `Brush.Accent.Secondary` | Hover on accent elements       |
| `Brush.Accent.Tertiary`  | Highlights, tag badges         |
| `Brush.Accent.Muted`     | Subtle accent backgrounds      |

### Borders

| Token                    | Usage               |
|--------------------------|---------------------|
| `Brush.Border.Default`   | Standard borders    |
| `Brush.Border.Strong`    | Emphasized borders  |
| `Brush.Border.Focus`     | Keyboard focus ring |
| `Brush.Border.Separator` | Dividers, hairlines |

### Syntax (use as `Color`, not `Brush`)

`Color.Syntax.Heading` · `Bold` · `Italic` · `Code` · `CodeBg`  
`Color.Syntax.Link` · `Quote` · `QuoteBorder` · `Marker`

### Status

`Color.Status.Success` · `Warning` · `Error` · `Info`

---

## Predefined Control Classes

| Class         | Control     | Description                             |
|---------------|-------------|-----------------------------------------|
| `Primary`     | Button      | Filled accent button                    |
| `Secondary`   | Button      | Outlined button                         |
| `Ghost`       | Button      | Transparent, hover only                 |
| `Toolbar`     | Button      | Square icon button (30×30)              |
| `EditorInput` | TextBox     | Bordered input with focus ring          |
| `CodeEditor`  | TextBox     | Full editor area (monospace)            |
| `EditorTabs`  | TabControl  | Editor/Preview tab strip                |
| `EditorTab`   | TabItem     | Individual tab with underline indicator |
| `FileItem`    | ListBoxItem | File tree row                           |
| `StatusBar`   | Border      | Bottom status strip                     |
| `Badge`       | Border      | Pill-shaped inline badge                |
| `Slim`        | ProgressBar | 2px slim progress indicator             |

---

## Color Palette Preview

### Light Theme "Vellichor Parchment"

- Base: `#F7F4EF`  Surface: `#FFFFFF`  Accent: `#C0622A`

### Dark Theme "Vellichor Ember"

- Base: `#141210`  Surface: `#1C1916`  Accent: `#E07A3E`
