using KarzounERP.Models;

namespace KarzounERP.Services.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> GetCustomersAsync(int companyId, string? search = null,
        ImportanceLevel? importance = null, FollowUpStage? stage = null);
    Task<Customer?> GetCustomerAsync(int id);
    Task<Customer> AddCustomerAsync(Customer customer);
    Task UpdateCustomerAsync(Customer customer);
    Task DeleteCustomerAsync(int id);
    Task<List<SalesDocument>> GetCustomerDocumentsAsync(int customerId);
    Task<List<Customer>> GetFollowUpRemindersAsync(int companyId);
    Task<List<CustomerNote>> GetNotesHistoryAsync(int customerId);
    Task<CustomerNote> AddNoteAsync(CustomerNote note);
    Task DeleteNoteAsync(int noteId);
}
