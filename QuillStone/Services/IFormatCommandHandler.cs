using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public interface IFormatCommandHandler
{
    void ApplyBold();
    void ApplyItalic();
    void ApplyInlineCode();
    Task InsertLinkAsync(Window owner);
    void ApplyHeading(int level);
    void ApplyBulletList();
    void ApplyNumberedList();
    void ApplyBlockquote();
    void ApplyCheckbox();
}

