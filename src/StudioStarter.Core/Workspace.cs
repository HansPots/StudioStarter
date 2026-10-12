namespace StudioStarter.Core;
public record WorkItem(Guid Id, string Title, bool IsDone);
public record Workspace(List<WorkItem> Tasks, string Notes, string DisplayName);
public interface IWorkspaceStore
{
    Task<Workspace?> LoadAsync();
    Task SaveAsync(Workspace workspace);
}
