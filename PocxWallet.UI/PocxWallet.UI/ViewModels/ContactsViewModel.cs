using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using PocxWallet.Core.Services;

namespace PocxWallet.UI.ViewModels;

public class ContactsViewModel : ViewModelBase
{
    private Contact? _selectedContact;
    private string _searchQuery = string.Empty;
    
    public ContactsViewModel()
    {
        AddContactCommand = ReactiveCommand.CreateFromTask(AddContact);
        EditContactCommand = ReactiveCommand.CreateFromTask<Contact>(EditContact);
        DeleteContactCommand = ReactiveCommand.CreateFromTask<Contact>(DeleteContact);
        SearchCommand = ReactiveCommand.Create(PerformSearch);
        
        Contacts = new ObservableCollection<Contact>();
    }
    
    public ObservableCollection<Contact> Contacts { get; }
    
    public Contact? SelectedContact
    {
        get => _selectedContact;
        set => this.RaiseAndSetIfChanged(ref _selectedContact, value);
    }
    
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchQuery, value);
            PerformSearch();
        }
    }
    
    public ReactiveCommand<Unit, Unit> AddContactCommand { get; }
    public ReactiveCommand<Contact, Unit> EditContactCommand { get; }
    public ReactiveCommand<Contact, Unit> DeleteContactCommand { get; }
    public ReactiveCommand<Unit, Unit> SearchCommand { get; }
    
    private async Task AddContact()
    {
        // TODO: Implement contact creation
        await Task.Delay(100);
    }
    
    private async Task EditContact(Contact contact)
    {
        // TODO: Implement contact editing
        await Task.Delay(100);
    }
    
    private async Task DeleteContact(Contact contact)
    {
        Contacts.Remove(contact);
        await Task.CompletedTask;
    }
    
    private void PerformSearch()
    {
        // TODO: Implement contact search
    }
}
