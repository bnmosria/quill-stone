using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using QuillStone.ViewModels;

namespace QuillStone.Controllers;

internal static class TreeViewHelper
{
    internal static FileSystemNodeViewModel? GetNodeFromVisual(Visual? visual)
    {
        if (visual is null)
            return null;

        var item = visual.FindAncestorOfType<TreeViewItem>(includeSelf: true);
        return item?.DataContext as FileSystemNodeViewModel;
    }

    internal static FolderNodeViewModel? GetDropTargetFolder(Visual? visual)
    {
        var node = GetNodeFromVisual(visual);
        return node switch
        {
            FolderNodeViewModel folder => folder,
            FileNodeViewModel file => file.ParentFolder,
            _ => null
        };
    }
}
