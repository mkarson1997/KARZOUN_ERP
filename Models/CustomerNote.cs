using System;
using System.ComponentModel.DataAnnotations;

namespace FornixxCRM.Models;

public class CustomerNote
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    [Required]
    public string NoteText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
}
