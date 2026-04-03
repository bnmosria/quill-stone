using QuillStone.Models;

namespace QuillStone.Services;

public interface IAppSettingsService
{
    AppSettings Settings { get; }
    Task LoadAsync();
    Task SaveAsync();
    void RecordProject(string name, string path);
    void RemoveStale();
}
