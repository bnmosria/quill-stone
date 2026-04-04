using System.Threading;
using Avalonia.Controls;
using QuillStone.Services;
using QuillStone.Views;

namespace QuillStone.Controllers;

internal sealed class PreviewController
{
    private readonly Panel _previewContainer;
    private readonly Border _previewPane;
    private readonly IMarkdownRenderService _renderService;
    private readonly IEditorService _editorService;
    private readonly Window _owner;

    private CancellationTokenSource _renderCts = new();
    private PreviewWindow? _previewWindow;

    public bool IsPreviewVisible => _previewPane.IsVisible;

    internal PreviewController(
        Panel previewContainer,
        Border previewPane,
        IMarkdownRenderService renderService,
        IEditorService editorService,
        Window owner)
    {
        _previewContainer = previewContainer;
        _previewPane = previewPane;
        _renderService = renderService;
        _editorService = editorService;
        _owner = owner;
    }

    public void OnEditorTextChanged()
    {
        if (IsPreviewVisible)
            _ = ScheduleUpdateAsync();

        _previewWindow?.UpdateContent(_editorService.GetEditorText());
    }

    public async Task ScheduleUpdateAsync()
    {
        var oldCts = _renderCts;
        _renderCts = new CancellationTokenSource();
        var token = _renderCts.Token;
        oldCts.Cancel();
        oldCts.Dispose();

        try
        {
            await Task.Delay(300, token);
            if (token.IsCancellationRequested)
                return;
            var markdown = _editorService.GetEditorText();
            var controls = _renderService.Render(markdown);
            PopulateContainer(controls);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    public void RenderImmediate()
    {
        CancelPendingRender();
        var markdown = _editorService.GetEditorText();
        var controls = _renderService.Render(markdown);
        PopulateContainer(controls);
    }

    public void RenderIfVisible()
    {
        if (IsPreviewVisible)
            RenderImmediate();
    }

    public void RenderIfEmpty()
    {
        if (_previewContainer.Children.Count == 0)
            RenderImmediate();
    }

    public void CancelPendingRender()
    {
        var oldCts = _renderCts;
        _renderCts = new CancellationTokenSource();
        oldCts.Cancel();
        oldCts.Dispose();
    }

    public void TogglePreviewWindow()
    {
        if (_previewWindow is not null)
        {
            _previewWindow.Close();
            return;
        }

        _previewWindow = new PreviewWindow();
        _previewWindow.Closed += (_, _) => _previewWindow = null;
        _previewWindow.Show(_owner);
        _previewWindow.UpdateContent(_editorService.GetEditorText());
    }

    private void PopulateContainer(IReadOnlyList<Control> controls)
    {
        _previewContainer.Children.Clear();
        foreach (var control in controls)
            _previewContainer.Children.Add(control);
    }
}
