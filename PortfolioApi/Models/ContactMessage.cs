using System.ComponentModel.DataAnnotations;
using Portfolio.Validation;

namespace Portfolio.Models;

/// <summary>
/// Corpo de <c>POST /api/contact</c>. Nunca é persistido — vai para o e-mail e para o log.
///
/// Os limites são de segurança, não de UX: sem eles o endpoint aceitava nome e mensagem de
/// tamanho ilimitado e e-mail em qualquer formato (C-5). Quem os executa é o
/// <see cref="ValidationFilter{T}"/> pendurado no endpoint — Minimal API não roda
/// DataAnnotations sozinha, então atributo sem filtro é decoração.
/// </summary>
public class ContactMessage
{
    // O texto de ErrorMessage vai direto para a tela do visitante (frontend lê `data.error`),
    // por isso é escrito à mão e em inglês, como o resto do site (design D-4).
    [Required(ErrorMessage = "Name is required.")]
    [NotBlank(ErrorMessage = "Name cannot be blank.")]
    [StringLength(80, ErrorMessage = "Name cannot exceed 80 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>254 é o máximo de um endereço de e-mail pela RFC 5321.</summary>
    [Required(ErrorMessage = "Email is required.")]
    [NotBlank(ErrorMessage = "Email cannot be blank.")]
    [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required.")]
    [NotBlank(ErrorMessage = "Message cannot be blank.")]
    [StringLength(4000, ErrorMessage = "Message cannot exceed 4000 characters.")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Honeypot (C-10). O formulário esconde o campo de pessoas; robô que preenche tudo o
    /// preenche. Sem validação de propósito: recusar com 400 ensinaria o robô a deixá-lo vazio.
    /// </summary>
    public string? Website { get; set; }
}
