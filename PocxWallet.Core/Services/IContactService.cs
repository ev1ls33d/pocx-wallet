namespace PocxWallet.Core.Services;

/// <summary>
/// Interface for contact management
/// </summary>
public interface IContactService
{
    /// <summary>
    /// Get all contacts
    /// </summary>
    IEnumerable<Contact> GetContacts();

    /// <summary>
    /// Add a new contact
    /// </summary>
    Contact AddContact(string name, string address, string? notes = null);

    /// <summary>
    /// Update contact information
    /// </summary>
    void UpdateContact(Contact contact);

    /// <summary>
    /// Delete a contact
    /// </summary>
    void DeleteContact(string contactId);

    /// <summary>
    /// Get contact by ID
    /// </summary>
    Contact? GetContact(string contactId);

    /// <summary>
    /// Search contacts by name or address
    /// </summary>
    IEnumerable<Contact> SearchContacts(string query);

    /// <summary>
    /// Get contact by address
    /// </summary>
    Contact? GetContactByAddress(string address);
}

public class Contact
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsed { get; set; }
}
