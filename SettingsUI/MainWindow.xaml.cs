using System.Windows;
using SettingsUI.ViewModels;

namespace SettingsUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Service.StartPolling();
            await viewModel.LoadAsync();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Service.StopPolling();
            viewModel.Service.Dispose();
        }
    }

    private void DatabaseTabView_Loaded(object sender, RoutedEventArgs e)
    {

    }
}
