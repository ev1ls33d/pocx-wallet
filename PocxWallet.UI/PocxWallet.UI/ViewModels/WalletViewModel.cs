using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;

namespace PocxWallet.UI.ViewModels;

public class WalletViewModel : ViewModelBase
{
    private string? _mnemonicPhrase;
    private bool _isWalletCreated;
    
    public WalletViewModel()
    {
        CreateWalletCommand = ReactiveCommand.CreateFromTask(CreateWallet);
        RestoreWalletCommand = ReactiveCommand.CreateFromTask(RestoreWallet);
        
        Addresses = new ObservableCollection<AddressItem>();
    }
    
    public string? MnemonicPhrase
    {
        get => _mnemonicPhrase;
        set => this.RaiseAndSetIfChanged(ref _mnemonicPhrase, value);
    }
    
    public bool IsWalletCreated
    {
        get => _isWalletCreated;
        set => this.RaiseAndSetIfChanged(ref _isWalletCreated, value);
    }
    
    public ObservableCollection<AddressItem> Addresses { get; }
    
    public ReactiveCommand<Unit, Unit> CreateWalletCommand { get; }
    public ReactiveCommand<Unit, Unit> RestoreWalletCommand { get; }
    
    private async Task CreateWallet()
    {
        // TODO: Implement wallet creation
        await Task.Delay(100);
    }
    
    private async Task RestoreWallet()
    {
        // TODO: Implement wallet restoration
        await Task.Delay(100);
    }
}

public class AddressItem
{
    public string Address { get; set; } = string.Empty;
    public uint Index { get; set; }
    public decimal Balance { get; set; }
}
