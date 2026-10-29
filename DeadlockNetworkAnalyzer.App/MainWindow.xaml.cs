using System.Windows;
using DeadlockNetworkAnalyzer.App.ViewModels;

namespace DeadlockNetworkAnalyzer.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closed += (_, _) => viewModel.Dispose();
    }
}
