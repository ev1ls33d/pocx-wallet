using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using PocxWallet.Core.Services;

namespace PocxWallet.UI.ViewModels;

public class AccountsViewModel : ViewModelBase
{
    private AccountInfo? _selectedAccount;
    
    public AccountsViewModel()
    {
        AddAccountCommand = ReactiveCommand.CreateFromTask(AddAccount);
        AddWatchOnlyCommand = ReactiveCommand.CreateFromTask(AddWatchOnly);
        DeleteAccountCommand = ReactiveCommand.CreateFromTask<AccountInfo>(DeleteAccount);
        
        Accounts = new ObservableCollection<AccountInfo>();
    }
    
    public ObservableCollection<AccountInfo> Accounts { get; }
    
    public AccountInfo? SelectedAccount
    {
        get => _selectedAccount;
        set => this.RaiseAndSetIfChanged(ref _selectedAccount, value);
    }
    
    public ReactiveCommand<Unit, Unit> AddAccountCommand { get; }
    public ReactiveCommand<Unit, Unit> AddWatchOnlyCommand { get; }
    public ReactiveCommand<AccountInfo, Unit> DeleteAccountCommand { get; }
    
    private async Task AddAccount()
    {
        // TODO: Implement account creation
        await Task.Delay(100);
    }
    
    private async Task AddWatchOnly()
    {
        // TODO: Implement watch-only account
        await Task.Delay(100);
    }
    
    private async Task DeleteAccount(AccountInfo account)
    {
        // TODO: Implement account deletion
        Accounts.Remove(account);
        await Task.CompletedTask;
    }
}
