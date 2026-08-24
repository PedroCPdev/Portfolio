namespace Portfolio.Models;

/// <summary>Corpo de <c>POST /api/admin/login</c>. Nunca é persistido nem logado.</summary>
public class LoginRequest
{
    public string Password { get; set; } = string.Empty;
}
