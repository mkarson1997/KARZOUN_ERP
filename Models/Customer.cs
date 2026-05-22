using System.ComponentModel.DataAnnotations;

namespace FornixxCRM.Models;

public class Customer
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    public CommercialMindset CommercialMindset { get; set; } = CommercialMindset.Simple;
    public FollowUpStage FollowUpStage { get; set; } = FollowUpStage.New;

    public string? CurrentObjection { get; set; }
    public string? Notes { get; set; }

    public ImportanceLevel Importance { get; set; } = ImportanceLevel.Normal;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastFollowUpDate { get; set; }
    public DateTime? NextFollowUpDate { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<SalesDocument> Documents { get; set; } = new List<SalesDocument>();
    public ICollection<CustomerNote> NotesHistory { get; set; } = new List<CustomerNote>();
}
