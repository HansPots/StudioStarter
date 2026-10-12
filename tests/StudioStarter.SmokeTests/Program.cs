using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging.Abstractions;
using StudioStarter.Core;
using StudioStarter.Desktop;
using StudioStarter.Infrastructure;

internal static class Program
{
    private static int checks;
    [STAThread]
    private static int Main(string[] args)
    {
        var folder = Path.Combine(Path.GetTempPath(), "StudioStarter-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "workspace.json");
            var store = new JsonWorkspaceStore(path);
            Check(store.LoadAsync().GetAwaiter().GetResult() is null, "Missing file starts fresh");
            var vm = new MainViewModel(store, NullLogger<MainViewModel>.Instance);
            vm.InitializeAsync().GetAwaiter().GetResult();
            Check(vm.TotalCount == 3 && vm.DoneCount == 1, "Initial dashboard counts");
            vm.NewTask = "  Test eigen module  "; vm.AddTaskCommand.Execute(null);
            Check(vm.Tasks.Last().Title == "Test eigen module" && vm.HasChanges, "Add task trims input and marks dirty");
            vm.Tasks.Last().IsDone = true;
            Check(vm.DoneCount == 2 && vm.Progress == 50, "Completion updates progress");
            vm.Search = "EIGEN MODULE";
            Check(vm.FilteredTasks.Count() == 1, "Search ignores case");
            vm.Notes = "Ideeën met accenten: café ☕"; vm.DisplayName = "Bouwer";
            vm.SaveCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            var restored = store.LoadAsync().GetAwaiter().GetResult()!;
            Check(restored.Tasks.Count == 4 && restored.Notes == vm.Notes && restored.DisplayName == "Bouwer" && !vm.HasChanges, "Persistence round trip");
            vm.DeleteTaskCommand.Execute(vm.Tasks.Last());
            Check(vm.TotalCount == 3 && vm.DoneCount == 1, "Delete updates dashboard");
            vm.NewTask = "  "; vm.AddTaskCommand.Execute(null);
            Check(vm.TotalCount == 3, "Empty task rejected");
            File.WriteAllText(path, "{invalid}");
            var damaged = new MainViewModel(store, NullLogger<MainViewModel>.Instance);
            damaged.InitializeAsync().GetAwaiter().GetResult();
            damaged.SaveCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            Check(File.ReadAllText(path) == "{invalid}", "Corrupt file is preserved");
            var failed = new MainViewModel(new FailingStore(), NullLogger<MainViewModel>.Instance);
            failed.Notes = "Keep this"; failed.SaveCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            Check(failed.HasChanges && failed.IsIdle && failed.Status.StartsWith("Opslaan mislukt"), "Save failure preserves dirty state");

            // Render actual WPF controls, without opening a desktop window.
            var app = new App(); app.InitializeComponent();
            vm.Search = "";
            var window = new MainWindow(vm);
            var surface = (FrameworkElement)window.Content;
            surface.DataContext = vm;
            System.Windows.Documents.TextElement.SetForeground(surface, new SolidColorBrush(Color.FromRgb(223, 231, 245)));
            surface.Width = 1120; surface.Height = 740;
            var size = new Size(1180, 800);
            surface.Measure(size); surface.Arrange(new Rect(size)); surface.UpdateLayout();
            Check(surface.ActualWidth == 1120, "WPF window constructs and lays out");
            if (args.Length > 0)
            {
                Directory.CreateDirectory(args[0]);
                for (var tab = 0; tab < 4; tab++)
                {
                    vm.SelectedTab = tab;
                    surface.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                    surface.UpdateLayout();
                    var bitmap = new RenderTargetBitmap(1180, 800, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(surface);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var file = File.Create(Path.Combine(args[0], $"preview-{tab}.png")); encoder.Save(file);
                }
            }
            Console.WriteLine($"PASS: {checks} checks");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(folder, recursive: true); }
    }
    private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
    private sealed class FailingStore : IWorkspaceStore
    {
        public Task<Workspace?> LoadAsync() => Task.FromResult<Workspace?>(null);
        public Task SaveAsync(Workspace workspace) => throw new IOException("Test failure");
    }
}
