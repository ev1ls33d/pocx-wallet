using PocxWallet.UI.Services;

namespace PocxWallet.UI.ViewModels;

/// <summary>
/// View model for the settings section
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    
    public SettingsViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }
}
