using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using QuillStone.Services;
using QuillStone.ViewModels;

namespace QuillStone.Controllers;

public sealed class DragDropController
{
    private readonly IProjectService _projectService;
    private readonly IDocumentService _documentService;
    private readonly IEditorService _editorService;
    private readonly IWindowDialogService _dialogService;
    private Window _owner = null!;
    private Action<FolderNodeViewModel?> _onMoveCompleted = null!;
    private Action _onTitleUpdateNeeded = null!;
    private TreeView? _projectTree;
    private FileSystemNodeViewModel? _pendingDragSource;
    private Point _pendingDragStartPoint;
    private FileSystemNodeViewModel? _activeDragSource;
    private const string DragNodeFormat = "QuillStone.Node";
    private const double DragThreshold = 8.0;

    public DragDropController(
        IProjectService projectService,
        IDocumentService documentService,
        IEditorService editorService,
        IWindowDialogService dialogService)
    {
        _projectService = projectService;
        _documentService = documentService;
        _editorService = editorService;
        _dialogService = dialogService;
    }

    internal void Wire(
        Window owner,
        Action<FolderNodeViewModel?> onMoveCompleted,
        Action onTitleUpdateNeeded)
    {
        _owner = owner;
        _onMoveCompleted = onMoveCompleted;
        _onTitleUpdateNeeded = onTitleUpdateNeeded;
    }
    public void Register(TreeView projectTree)
    {
        _projectTree = projectTree;
        projectTree.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        projectTree.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        projectTree.AddHandler(DragDrop.DropEvent, OnDrop);
        projectTree.AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            return;
        var node = TreeViewHelper.GetNodeFromVisual(e.Source as Visual);
        if (node is null)
            return;
        if (node is FolderNodeViewModel folder && IsProjectRoot(folder))
            return;
        _pendingDragSource = node;
        _pendingDragStartPoint = e.GetCurrentPoint(_projectTree).Position;
    }
    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pendingDragSource is null)
            return;
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            _pendingDragSource = null;
            return;
        }
        var current = e.GetCurrentPoint(_projectTree).Position;
        var dx = current.X - _pendingDragStartPoint.X;
        var dy = current.Y - _pendingDragStartPoint.Y;
        if (Math.Abs(dx) < DragThreshold && Math.Abs(dy) < DragThreshold)
            return;
        var source = _pendingDragSource;
        _pendingDragSource = null;
        _activeDragSource = source;
        try
        {
            var data = new DataObject();
            data.Set(DragNodeFormat, source);
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone",
                $"An unexpected error occurred while starting the drag.\n\n{ex.Message}");
        }
        finally { _activeDragSource = null; }
    }
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var target = TreeViewHelper.GetDropTargetFolder(e.Source as Visual);
        if (_activeDragSource is null || target is null || !IsValidDropTarget(_activeDragSource, target))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var source = _activeDragSource;
        var target = TreeViewHelper.GetDropTargetFolder(e.Source as Visual);
        if (source is null || target is null || !IsValidDropTarget(source, target))
            return;
        e.Handled = true;
        try
        { await MoveNodeToFolderAsync(source, target); }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone",
                $"An unexpected error occurred during the drop.\n\n{ex.Message}");
        }
    }
    private async Task MoveNodeToFolderAsync(FileSystemNodeViewModel source, FolderNodeViewModel target)
    {
        var destPath = Path.Combine(target.FullPath, source.Name);
        var sourcePath = source.FullPath;
        bool isFolder = source is FolderNodeViewModel;
        bool destExists = isFolder ? Directory.Exists(destPath) : File.Exists(destPath);
        if (destExists)
        {
            var choice = await _dialogService.ShowConfirmDialogAsync(
                _owner, "QuillStone",
                $"'{source.Name}' already exists in the target folder. What would you like to do?",
                "Overwrite", "Skip", "Cancel");
            if (choice != DialogChoice.Primary)
                return;
            try
            {
                if (isFolder)
                    Directory.Delete(destPath, recursive: true);
                else
                    File.Delete(destPath);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone",
                    $"Could not remove the existing item before moving.\n\n{ex.Message}");
                return;
            }
        }
        var sourceParent = source.ParentFolder;
        string? newOpenFilePath = isFolder ? GetNewOpenFilePathAfterFolderMove(sourcePath, destPath) : null;
        bool isCurrentFile = !isFolder && source is FileNodeViewModel fileVm && IsCurrentlyOpenFile(fileVm);
        try
        {
            if (isFolder)
                Directory.Move(sourcePath, destPath);
            else
                File.Move(sourcePath, destPath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageDialogAsync(_owner, "QuillStone",
                $"Could not move '{source.Name}'.\n\n{ex.Message}");
            return;
        }
        if (isCurrentFile && await _documentService.RebindCurrentFileAsync(_owner, destPath, _editorService.GetEditorText()))
            _onTitleUpdateNeeded();
        else if (newOpenFilePath is not null && await _documentService.RebindCurrentFileAsync(_owner, newOpenFilePath, _editorService.GetEditorText()))
            _onTitleUpdateNeeded();
        sourceParent?.Refresh();
        var targetIsSameAsSource = sourceParent is not null && string.Equals(sourceParent.FullPath, target.FullPath, StringComparison.OrdinalIgnoreCase);
        if (!targetIsSameAsSource)
            target.Refresh();
        _onMoveCompleted(sourceParent);
    }
    private bool IsValidDropTarget(FileSystemNodeViewModel source, FolderNodeViewModel target)
    {
        var sourceParentPath = source.ParentFolder?.FullPath;
        if (sourceParentPath is not null && string.Equals(sourceParentPath, target.FullPath, StringComparison.OrdinalIgnoreCase))
            return false;
        if (source is FolderNodeViewModel sourceFolder && IsFolderDescendantOrSelf(sourceFolder, target))
            return false;
        return true;
    }
    private static bool IsFolderDescendantOrSelf(FolderNodeViewModel candidate, FolderNodeViewModel target)
    {
        var candidatePath = Path.GetFullPath(candidate.FullPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetPath = Path.GetFullPath(target.FullPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return targetPath.StartsWith(candidatePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidatePath, targetPath, StringComparison.OrdinalIgnoreCase);
    }
    private bool IsCurrentlyOpenFile(FileNodeViewModel fileNode)
    {
        var currentPath = _documentService.CurrentDocument?.LocalPath;
        return currentPath is not null
            && string.Equals(currentPath, fileNode.FullPath, StringComparison.OrdinalIgnoreCase);
    }
    private string? GetNewOpenFilePathAfterFolderMove(string sourceFolderPath, string destFolderPath)
    {
        var currentPath = _documentService.CurrentDocument?.LocalPath;
        if (currentPath is null)
            return null;
        var normalizedCurrent = Path.GetFullPath(currentPath);
        var normalizedSource = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceFolderPath))
            + Path.DirectorySeparatorChar;
        if (!normalizedCurrent.StartsWith(normalizedSource, StringComparison.OrdinalIgnoreCase))
            return null;
        var relative = normalizedCurrent[normalizedSource.Length..];
        return Path.Combine(destFolderPath, relative);
    }
    private bool IsProjectRoot(FolderNodeViewModel folder)
        => _projectService.CurrentProject is { } project
            && string.Equals(project.RootPath, folder.FullPath, StringComparison.OrdinalIgnoreCase);
}
