using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using QuillStone.Services;
using QuillStone.Styles.Theme;

namespace QuillStone.Views;

public partial class ReaderWindow : Window
{
    private readonly IMarkdownRenderService _renderService;
    private CancellationTokenSource _updateCts = new();
    private double _readerFontSize = 16;
    private bool _focusMode;
    private List<(int Level, Control Control)> _headings = [];

    // Design-time support — creates a default render service
    public ReaderWindow() : this(new MarkdownRenderService()) { }

    public ReaderWindow(IMarkdownRenderService renderService)
    {
        InitializeComponent();
        _renderService = renderService;
        PrevHeadingButton.IsEnabled = false;
        NextHeadingButton.IsEnabled = false;
    }

    public void LoadContent(string markdown, string? documentName, string? basePath)
    {
        SetDocumentName(documentName);
        RenderContent(markdown, basePath);
        ReaderScroll.ScrollToHome();
    }

    public void UpdateContent(string markdown, string? documentName, string? basePath)
    {
        SetDocumentName(documentName);
        _ = ScheduleUpdateAsync(markdown, basePath);
    }

    public void SetDocumentName(string? documentName) =>
        DocumentNameLabel.Text = documentName ?? "Untitled";

    private async Task ScheduleUpdateAsync(string markdown, string? basePath)
    {
        var oldCts = _updateCts;
        _updateCts = new CancellationTokenSource();
        var token = _updateCts.Token;
        oldCts.Cancel();
        oldCts.Dispose();

        try
        {
            await Task.Delay(300, token);
            if (token.IsCancellationRequested)
                return;
            RenderContent(markdown, basePath);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private void RenderContent(string markdown, string? basePath)
    {
        var controls = _renderService.Render(markdown, basePath);

        foreach (var child in ReaderContainer.Children)
        {
            if (child is Image { Source: IDisposable bitmap })
                bitmap.Dispose();
        }

        ReaderContainer.Children.Clear();
        ReaderContainer.SetValue(TextElement.FontSizeProperty, _readerFontSize);
        foreach (var control in controls)
            ReaderContainer.Children.Add(control);

        BuildToc(controls);
        UpdateNavigationState();
    }

    // ── TOC ──────────────────────────────────────────────────────────

    private void BuildToc(IReadOnlyList<Control> controls)
    {
        TocContainer.Children.Clear();
        _headings.Clear();

        foreach (var control in controls)
        {
            if (control is not TextBlock tb)
                continue;

            int level = 0;
            if (tb.Classes.Contains("MdH1"))
                level = 1;
            else if (tb.Classes.Contains("MdH2"))
                level = 2;
            else if (tb.Classes.Contains("MdH3"))
                level = 3;
            if (level == 0)
                continue;

            _headings.Add((level, tb));

            var btn = new Button { Classes = { "TocItem" } };
            if (level == 2)
                btn.Margin = new Thickness(10, 0, 0, 0);
            else if (level == 3)
                btn.Margin = new Thickness(20, 0, 0, 0);

            btn.Content = new TextBlock
            {
                Text = ExtractText(tb),
                TextWrapping = TextWrapping.Wrap,
            };

            var captured = tb;
            btn.Click += (_, _) => ScrollToHeading(captured);
            TocContainer.Children.Add(btn);
        }
    }

    private static string ExtractText(TextBlock tb)
    {
        if (tb.Inlines is { Count: > 0 })
        {
            var sb = new StringBuilder();
            AppendInlines(sb, tb.Inlines);
            return sb.ToString().Trim();
        }
        return tb.Text ?? string.Empty;
    }

    private static void AppendInlines(StringBuilder sb, InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            if (inline is Run run)
                sb.Append(run.Text);
            else if (inline is Span span)
                AppendInlines(sb, span.Inlines);
        }
    }

    // ── NAVIGATION ───────────────────────────────────────────────────

    private double? GetControlY(Control control) =>
        control.TranslatePoint(new Point(0, 0), ReaderContent)?.Y;

    private void ScrollToHeading(Control headingControl)
    {
        var y = GetControlY(headingControl);
        if (y.HasValue)
            ReaderScroll.Offset = new Vector(0, y.Value);
    }

    private int GetActiveHeadingIndex()
    {
        var scrollY = ReaderScroll.Offset.Y;
        int active = 0;
        for (int i = 0; i < _headings.Count; i++)
        {
            var y = GetControlY(_headings[i].Control);
            if (y.HasValue && y.Value <= scrollY + 10)
                active = i;
        }
        return active;
    }

    private bool IsLastSectionFullyVisible()
    {
        if (_headings.Count == 0)
            return true;
        return ReaderScroll.Offset.Y + ReaderScroll.Viewport.Height >= ReaderScroll.Extent.Height - 10;
    }

    private void UpdateNavigationState()
    {
        PrevHeadingButton.IsEnabled = _headings.Count > 0 && ReaderScroll.Offset.Y > 1;
        NextHeadingButton.IsEnabled = _headings.Count > 0 && !IsLastSectionFullyVisible();
    }

    private void PrevHeading_Click(object? sender, RoutedEventArgs e)
    {
        if (_headings.Count == 0)
            return;
        var scrollY = ReaderScroll.Offset.Y;
        var viewport = ReaderScroll.Viewport.Height;
        int active = GetActiveHeadingIndex();
        double activeY = GetControlY(_headings[active].Control) ?? 0;

        if (scrollY > activeY + 10)
        {
            ReaderScroll.Offset = new Vector(0, Math.Max(0, scrollY - viewport));
            return;
        }

        if (active > 0)
            ScrollToHeading(_headings[active - 1].Control);
    }

    private void NextHeading_Click(object? sender, RoutedEventArgs e)
    {
        if (_headings.Count == 0)
            return;
        var scrollY = ReaderScroll.Offset.Y;
        var viewport = ReaderScroll.Viewport.Height;
        int active = GetActiveHeadingIndex();

        double nextY = active + 1 < _headings.Count
            ? GetControlY(_headings[active + 1].Control) ?? ReaderScroll.Extent.Height
            : ReaderScroll.Extent.Height;

        if (nextY > scrollY + viewport)
            ReaderScroll.Offset = new Vector(0, scrollY + viewport);
        else if (active + 1 < _headings.Count)
            ScrollToHeading(_headings[active + 1].Control);
    }

    // ── SCROLL PROGRESS ──────────────────────────────────────────────

    private void ReaderScroll_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var scrollable = ReaderScroll.Extent.Height - ReaderScroll.Viewport.Height;
        var progress = scrollable > 0
            ? Math.Clamp(ReaderScroll.Offset.Y / scrollable * 100, 0, 100)
            : 100;

        ReadingProgress.Value = progress;
        SidebarProgress.Value = progress;
        ProgressLabel.Text = $"{(int)progress}% complete";

        UpdateActiveTocItem();
        UpdateNavigationState();
    }

    private void UpdateActiveTocItem()
    {
        if (_headings.Count == 0 || TocContainer.Children.Count == 0)
            return;
        int active = GetActiveHeadingIndex();

        for (int i = 0; i < TocContainer.Children.Count; i++)
        {
            if (TocContainer.Children[i] is not Button btn)
                continue;
            if (i == active)
                btn.Classes.Add("TocItemActive");
            else
                btn.Classes.Remove("TocItemActive");
        }
    }

    // ── TOP-BAR ACTIONS ──────────────────────────────────────────────

    private void FontSizeDecrease_Click(object? sender, RoutedEventArgs e)
    {
        _readerFontSize = Math.Max(12, _readerFontSize - 2);
        ReaderContainer.SetValue(TextElement.FontSizeProperty, _readerFontSize);
    }

    private void FontSizeIncrease_Click(object? sender, RoutedEventArgs e)
    {
        _readerFontSize = Math.Min(24, _readerFontSize + 2);
        ReaderContainer.SetValue(TextElement.FontSizeProperty, _readerFontSize);
    }

    private void ToggleTheme_Click(object? sender, RoutedEventArgs e) => ThemeManager.Toggle();

    private void FocusToggle_Click(object? sender, RoutedEventArgs e)
    {
        _focusMode = !_focusMode;
        TocSidebar.IsVisible = !_focusMode;
        if (_focusMode)
        {
            FocusToggleButton.Classes.Add("ViewModeActive");
            ReaderScroll.Focus();
        }
        else
        {
            FocusToggleButton.Classes.Remove("ViewModeActive");
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
