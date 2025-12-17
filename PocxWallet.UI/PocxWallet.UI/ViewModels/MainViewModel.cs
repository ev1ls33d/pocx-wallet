using ReactiveUI;
using System.Reactive;

namespace PocxWallet.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentPage;
    
    public MainViewModel()
    {
        // Start with the dashboard
        _currentPage = new DashboardViewModel();
        
        // Navigation commands
        ShowDashboardCommand = ReactiveCommand.Create(ShowDashboard);
        ShowWalletCommand = ReactiveCommand.Create(ShowWallet);
        ShowSendCommand = ReactiveCommand.Create(ShowSend);
        ShowReceiveCommand = ReactiveCommand.Create(ShowReceive);
        ShowTransactionsCommand = ReactiveCommand.Create(ShowTransactions);
        ShowAccountsCommand = ReactiveCommand.Create(ShowAccounts);
        ShowContactsCommand = ReactiveCommand.Create(ShowContacts);
        ShowSettingsCommand = ReactiveCommand.Create(ShowSettings);
    }
    
    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }
    
    // Navigation Commands
    public ReactiveCommand<Unit, Unit> ShowDashboardCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowWalletCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSendCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowReceiveCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowTransactionsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAccountsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowContactsCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; }
    
    private void ShowDashboard()
    {
        CurrentPage = new DashboardViewModel();
    }
    
    private void ShowWallet()
    {
        CurrentPage = new WalletViewModel();
    }
    
    private void ShowSend()
    {
        CurrentPage = new SendViewModel();
    }
    
    private void ShowReceive()
    {
        CurrentPage = new ReceiveViewModel();
    }
    
    private void ShowTransactions()
    {
        CurrentPage = new TransactionsViewModel();
    }
    
    private void ShowAccounts()
    {
        CurrentPage = new AccountsViewModel();
    }
    
    private void ShowContacts()
    {
        CurrentPage = new ContactsViewModel();
    }
    
    private void ShowSettings()
    {
        CurrentPage = new SettingsViewModel();
    }
}
