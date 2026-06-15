using System.ComponentModel.DataAnnotations;

namespace Portfolio.Models;

public class ContactMessage
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Message { get; set; } = string.Empty;
}
