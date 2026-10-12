using System.Windows;
namespace StudioStarter.Desktop;
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent(); DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeAsync();
        Closing += (_, e) =>
        {
            if (viewModel.IsBusy) { e.Cancel = true; return; }
            if (viewModel.HasChanges && MessageBox.Show("Je hebt niet-opgeslagen wijzigingen. Toch afsluiten?", "Studio", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) e.Cancel = true;
        };
    }
}
