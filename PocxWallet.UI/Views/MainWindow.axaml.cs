using Avalonia.Controls;
using Avalonia.Interactivity;
using PocxWallet.UI.ViewModels;

namespace PocxWallet.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NavigationList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && e.AddedItems.Count > 0)
        {
            if (e.AddedItems[0] is NavigationItem item)
            {
                viewModel.NavigateCommand.Execute(item);
            }
        }
    }
}
