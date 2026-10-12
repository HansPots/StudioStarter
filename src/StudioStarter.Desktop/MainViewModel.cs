using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using StudioStarter.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
namespace StudioStarter.Desktop;

public partial class TaskItemViewModel(Guid id, string title, bool done) : ObservableObject
{
    public Guid Id { get; } = id;
    public string Title { get; } = title;
    [ObservableProperty] private bool isDone = done;
}
public partial class MainViewModel : ObservableObject
{
    private readonly IWorkspaceStore store;
    private readonly ILogger<MainViewModel> logger;
    private bool loadFailed;
    public ObservableCollection<TaskItemViewModel> Tasks { get; } = [];
    [ObservableProperty] private string newTask = "";
    [ObservableProperty] private string search = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private string displayName = "Maker";
    [ObservableProperty] private string status = "Welkom in jouw werkruimte";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool hasChanges;
    [ObservableProperty] private int selectedTab;
    public bool IsIdle => !IsBusy;
    public string Greeting => $"Hallo, {DisplayName}.";
    public string SaveLabel => HasChanges ? "Niet-opgeslagen wijzigingen" : "Alles bijgewerkt";
    public int TotalCount => Tasks.Count;
    public int DoneCount => Tasks.Count(t => t.IsDone);
    public int OpenCount => TotalCount - DoneCount;
    public double Progress => TotalCount == 0 ? 0 : 100.0 * DoneCount / TotalCount;
    public IEnumerable<TaskItemViewModel> FilteredTasks => Tasks.Where(t => t.Title.Contains(Search, StringComparison.OrdinalIgnoreCase));
    public MainViewModel(IWorkspaceStore store, ILogger<MainViewModel> logger) { this.store = store; this.logger = logger; }
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsIdle));
    partial void OnHasChangesChanged(bool value) => OnPropertyChanged(nameof(SaveLabel));
    partial void OnDisplayNameChanged(string value) { OnPropertyChanged(nameof(Greeting)); HasChanges = true; }
    partial void OnNotesChanged(string value) => HasChanges = true;
    partial void OnSearchChanged(string value) => OnPropertyChanged(nameof(FilteredTasks));
    private void UpdateCounts()
    {
        foreach (var name in new[] { nameof(TotalCount), nameof(DoneCount), nameof(OpenCount), nameof(Progress), nameof(FilteredTasks) }) OnPropertyChanged(name);
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) { HasChanges = true; UpdateCounts(); }
    private void Attach(WorkItem item) { var vm = new TaskItemViewModel(item.Id, item.Title, item.IsDone); vm.PropertyChanged += ItemChanged; Tasks.Add(vm); }
    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var state = await store.LoadAsync() ?? new Workspace([new(Guid.NewGuid(), "Verken het dashboard", true), new(Guid.NewGuid(), "Voeg je eerste eigen taak toe", false), new(Guid.NewGuid(), "Bouw een nieuwe pagina", false)], "Mijn ideeën\n\n• Een projectoverzicht toevoegen\n• Een API aansluiten\n• Eigen componenten ontwerpen", "Maker");
            foreach (var item in state.Tasks) Attach(item);
            Notes = state.Notes; DisplayName = state.DisplayName; UpdateCounts(); HasChanges = false;
        }
        catch (Exception ex) { loadFailed = true; Status = $"Laden mislukt: {ex.Message}. Maak een back-up en herstel het werkruimtebestand voordat je opnieuw opslaat."; }
        finally { IsBusy = false; }
    }
    [RelayCommand] private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTask)) { Status = "Vul eerst een taak in."; return; }
        Attach(new WorkItem(Guid.NewGuid(), NewTask.Trim(), false)); NewTask = ""; HasChanges = true; UpdateCounts(); Status = "Taak toegevoegd";
    }
    [RelayCommand] private void DeleteTask(TaskItemViewModel? item)
    {
        if (item is null) return;
        item.PropertyChanged -= ItemChanged; Tasks.Remove(item); HasChanges = true; UpdateCounts(); Status = "Taak verwijderd";
    }
    private Workspace Snapshot() => new(Tasks.Select(t => new WorkItem(t.Id, t.Title, t.IsDone)).ToList(), Notes, DisplayName);
    [RelayCommand] private async Task SaveAsync()
    {
        if (IsBusy) return;
        if (loadFailed) { Status = "Opslaan geblokkeerd: herstel eerst het bestaande werkruimtebestand. Exporteren blijft beschikbaar."; return; }
        IsBusy = true;
        try { await store.SaveAsync(Snapshot()); HasChanges = false; Status = "Werkruimte lokaal opgeslagen"; logger.LogInformation("Workspace saved with {Count} tasks", Tasks.Count); }
        catch (Exception ex) { Status = $"Opslaan mislukt: {ex.Message}"; logger.LogWarning("Save failed: {Type}", ex.GetType().Name); }
        finally { IsBusy = false; }
    }
    [RelayCommand] private async Task ExportAsync()
    {
        var dialog = new SaveFileDialog { Filter = "JSON-bestand|*.json", FileName = "werkruimte.json" };
        if (dialog.ShowDialog() != true) return;
        try { await File.WriteAllTextAsync(dialog.FileName, JsonSerializer.Serialize(Snapshot(), new JsonSerializerOptions { WriteIndented = true })); Status = "Werkruimte geëxporteerd"; }
        catch (Exception ex) { Status = $"Export mislukt: {ex.Message}"; }
    }
}
