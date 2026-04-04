using QuillStone.Models;

namespace QuillStone.Services;

public interface IAppSettingsService
{
    AppSettings Settings { get; }
    Task LoadAsync();
    Task SaveAsync();
    Task ResetToDefaultsAsync();
    void RecordProject(string name, string path);
    void RemoveStale();
}
