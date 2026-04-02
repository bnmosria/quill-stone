using Avalonia.Platform.Storage;

namespace QuillStone.Models;

public sealed record LoadedDocument(IStorageFile File, string? LocalPath, string Content);

