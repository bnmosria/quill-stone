namespace QuillStone.Models;

public readonly record struct TextSelectionRange(int Start, int End)
{
    public int NormalizedStart => Math.Min(Start, End);
    public int NormalizedEnd => Math.Max(Start, End);
    public bool HasSelection => NormalizedEnd > NormalizedStart;
}

