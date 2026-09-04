using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KarzounERP.Models;

public class Customer : ObservableObject
{
    private bool _isSelected;

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
    [MaxLength(20)]
    public string? ColorMarker { get; set; }

    private string? _displayColorMarker;

    [NotMapped]
    public string? DisplayColorMarker
    {
        get => _displayColorMarker;
        set => SetProperty(ref _displayColorMarker, value);
    }

    [NotMapped]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastFollowUpDate { get; set; }
    public DateTime? NextFollowUpDate { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<SalesDocument> Documents { get; set; } = new List<SalesDocument>();
    public ICollection<CustomerNote> NotesHistory { get; set; } = new List<CustomerNote>();
}
