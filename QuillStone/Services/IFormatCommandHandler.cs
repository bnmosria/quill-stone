using QuillStone.Models;

namespace QuillStone.Services;

public interface IFormatCommandHandler
{
    void ApplyBold();
    void ApplyItalic();
    void ApplyInlineCode();
    void ApplyStrikethrough();
    void InsertLink();
    void ApplyHeading(int level);
    void ApplyBulletList();
    void ApplyNumberedList();
    void ApplyBlockquote();
    void ApplyCheckbox();
    void ApplyCodeBlock();
    void InsertImage();
}

