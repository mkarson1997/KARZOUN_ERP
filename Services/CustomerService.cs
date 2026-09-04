using KarzounERP.Data;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KarzounERP.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context) => _context = context;

    public async Task<List<Customer>> GetCustomersAsync(int companyId, string? search = null,
        ImportanceLevel? importance = null, FollowUpStage? stage = null)
    {
        var query = _context.Customers.Where(c => c.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                (c.Phone != null && c.Phone.Contains(s)) ||
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)) ||
                (c.Country != null && c.Country.ToLower().Contains(s)));
        }

        if (importance.HasValue) query = query.Where(c => c.Importance == importance.Value);
        if (stage.HasValue) query = query.Where(c => c.FollowUpStage == stage.Value);

        var list = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            list = list.OrderBy(c =>
            {
                if (c.FullName.Equals(s, StringComparison.OrdinalIgnoreCase)) return 0;
                if (c.FullName.StartsWith(s, StringComparison.OrdinalIgnoreCase)) return 1;
                if (c.FullName.Contains(s, StringComparison.OrdinalIgnoreCase)) return 2;
                if (c.CompanyName != null && c.CompanyName.Contains(s, StringComparison.OrdinalIgnoreCase)) return 3;
                return 4;
            })
            .ThenBy(c => c.FullName)
            .ToList();
        }
        else
        {
            list = list.OrderBy(c => c.FullName).ToList();
        }

        return list;
    }

    public async Task<Customer?> GetCustomerAsync(int id)
        => await _context.Customers.FindAsync(id);

    public async Task<Customer> AddCustomerAsync(Customer customer)
    {
        customer.CreatedAt = DateTime.UtcNow;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task UpdateCustomerAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCustomerAsync(int id)
    {
        var c = await _context.Customers.FindAsync(id);
        if (c != null)
        {
            _context.Customers.Remove(c);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<SalesDocument>> GetCustomerDocumentsAsync(int customerId)
        => await _context.Documents
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.Date)
            .ToListAsync();

    public async Task<List<Customer>> GetFollowUpRemindersAsync(int companyId)
    {
        var today = DateTime.Today;
        return await _context.Customers
            .Where(c => c.CompanyId == companyId && c.NextFollowUpDate != null && c.NextFollowUpDate <= today)
            .OrderBy(c => c.NextFollowUpDate)
            .ToListAsync();
    }

    public async Task<List<CustomerNote>> GetNotesHistoryAsync(int customerId)
    {
        return await _context.CustomerNotes
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<CustomerNote> AddNoteAsync(CustomerNote note)
    {
        note.CreatedAt = DateTime.UtcNow;
        _context.CustomerNotes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task DeleteNoteAsync(int noteId)
    {
        var note = await _context.CustomerNotes.FindAsync(noteId);
        if (note != null)
        {
            _context.CustomerNotes.Remove(note);
            await _context.SaveChangesAsync();
        }
    }
}
