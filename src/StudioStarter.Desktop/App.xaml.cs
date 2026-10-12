using System.IO;
using System.Windows;
using StudioStarter.Core;
using StudioStarter.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
namespace StudioStarter.Desktop;
public partial class App : Application
{
    private IHost? host;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StudioStarter");
        host = Host.CreateDefaultBuilder()
            .UseSerilog((context, config) => config.MinimumLevel.Information().WriteTo.File(Path.Combine(data, "logs", "app-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7))
            .ConfigureServices(services =>
            {
                services.AddHttpClient();
                services.AddSingleton<IWorkspaceStore>(new JsonWorkspaceStore(Path.Combine(data, "workspace.json")));
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            }).Build();
        host.Services.GetRequiredService<MainWindow>().Show();
    }
    protected override void OnExit(ExitEventArgs e) { host?.Dispose(); Log.CloseAndFlush(); base.OnExit(e); }
}
