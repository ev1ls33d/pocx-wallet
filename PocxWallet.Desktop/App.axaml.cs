using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PocxWallet.UI.Views;
using PocxWallet.UI.ViewModels;
using PocxWallet.UI.Services;

namespace PocxWallet.Desktop;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create service instances
            var walletService = new WalletService();
            var dockerService = new DockerService();
            var configurationService = new ConfigurationService();
            
            // Create main view model
            var mainViewModel = new MainWindowViewModel(
                walletService,
                dockerService,
                configurationService);
            
            // Create and show main window
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
