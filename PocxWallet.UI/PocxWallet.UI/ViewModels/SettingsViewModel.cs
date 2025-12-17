using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;

namespace PocxWallet.UI.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private string _nodeUrl = "http://localhost:8332";
    private bool _autoSelectNode = true;
    private string _selectedLanguage = "English";
    private bool _enableBiometrics;
    private bool _useDarkTheme;
    
    public SettingsViewModel()
    {
        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettings);
        TestNodeConnectionCommand = ReactiveCommand.CreateFromTask(TestNodeConnection);
        
        AvailableLanguages = new ObservableCollection<string>
        {
            "English", "Spanish", "German", "French", "Chinese", "Japanese",
            "Portuguese", "Russian", "Italian", "Dutch", "Polish"
        };
    }
    
    public string NodeUrl
    {
        get => _nodeUrl;
        set => this.RaiseAndSetIfChanged(ref _nodeUrl, value);
    }
    
    public bool AutoSelectNode
    {
        get => _autoSelectNode;
        set => this.RaiseAndSetIfChanged(ref _autoSelectNode, value);
    }
    
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set => this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
    }
    
    public bool EnableBiometrics
    {
        get => _enableBiometrics;
        set => this.RaiseAndSetIfChanged(ref _enableBiometrics, value);
    }
    
    public bool UseDarkTheme
    {
        get => _useDarkTheme;
        set => this.RaiseAndSetIfChanged(ref _useDarkTheme, value);
    }
    
    public ObservableCollection<string> AvailableLanguages { get; }
    
    public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> TestNodeConnectionCommand { get; }
    
    private async Task SaveSettings()
    {
        // TODO: Implement settings save
        await Task.Delay(100);
    }
    
    private async Task TestNodeConnection()
    {
        // TODO: Implement node connection test
        await Task.Delay(100);
    }
}
