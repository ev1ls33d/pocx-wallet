using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;

namespace PocxWallet.UI.ViewModels;

public class TransactionsViewModel : ViewModelBase
{
    private TransactionItem? _selectedTransaction;
    
    public TransactionsViewModel()
    {
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshTransactions);
        
        Transactions = new ObservableCollection<TransactionItem>();
        
        // Load initial data
        _ = RefreshTransactions();
    }
    
    public ObservableCollection<TransactionItem> Transactions { get; }
    
    public TransactionItem? SelectedTransaction
    {
        get => _selectedTransaction;
        set => this.RaiseAndSetIfChanged(ref _selectedTransaction, value);
    }
    
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    
    private async Task RefreshTransactions()
    {
        // TODO: Implement transaction fetching
        await Task.Delay(100);
    }
}

public class TransactionItem
{
    public string TxId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty; // Send, Receive, MultiOut
    public decimal Amount { get; set; }
    public string? FromAddress { get; set; }
    public string? ToAddress { get; set; }
    public int Confirmations { get; set; }
    public string? Message { get; set; }
}
