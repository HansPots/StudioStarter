using System.Text.Json;
using StudioStarter.Core;
namespace StudioStarter.Infrastructure;
public sealed class JsonWorkspaceStore(string path) : IWorkspaceStore
{
    public async Task<Workspace?> LoadAsync()
    {
        if (!File.Exists(path)) return null;
        var state = JsonSerializer.Deserialize<Workspace>(await File.ReadAllTextAsync(path));
        if (state?.Tasks is null || state.Notes is null || state.DisplayName is null || state.Tasks.Any(t => t is null || string.IsNullOrWhiteSpace(t.Title))) throw new JsonException("Ongeldig werkruimtebestand.");
        return state;
    }
    public async Task SaveAsync(Workspace workspace)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(workspace, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
