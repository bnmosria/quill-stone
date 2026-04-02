using System.Text;
using Avalonia.Platform.Storage;
using QuillStone.Models;

namespace QuillStone.Services;

public sealed class MarkdownFileService : IMarkdownFileService
{
    public async Task<LoadedDocument> LoadAsync(IStorageFile file)
    {
        string content;
        string? localPath = file.TryGetLocalPath();

        if (localPath is not null)
        {
            content = await File.ReadAllTextAsync(localPath, Encoding.UTF8);
        }
        else
        {
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            content = await reader.ReadToEndAsync();
        }

        return new LoadedDocument(file, localPath, content);
    }

    public async Task<string?> SaveAsync(IStorageFile file, string content)
    {
        string? localPath = file.TryGetLocalPath();

        if (localPath is not null)
        {
            await File.WriteAllTextAsync(localPath, content, Encoding.UTF8);
        }
        else
        {
            await using var stream = await file.OpenWriteAsync();
            if (stream.CanSeek)
                stream.SetLength(0);

            await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
        }

        return localPath;
    }
}

