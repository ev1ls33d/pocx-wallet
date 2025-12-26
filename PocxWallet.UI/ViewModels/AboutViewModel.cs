using CommunityToolkit.Mvvm.ComponentModel;

namespace PocxWallet.UI.ViewModels;

/// <summary>
/// View model for the about section
/// </summary>
public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _version = "0.7.3";
    
    [ObservableProperty]
    private string _description = "A comprehensive cross-platform HD Wallet for PoCX with full node integration, plotting, mining, and transaction capabilities";
    
    [ObservableProperty]
    private string _copyright = "Copyright © 2025 EviLSeeD";
    
    [ObservableProperty]
    private string _license = "MIT License";
    
    [ObservableProperty]
    private string _githubUrl = "github.com/ev1ls33d/pocx-wallet";
    
    [ObservableProperty]
    private string _documentationUrl = "View README.md";
}
