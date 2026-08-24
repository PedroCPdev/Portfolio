using System.ComponentModel.DataAnnotations;

namespace Portfolio.Validation;

/// <summary>
/// Exige conteúdo de verdade. <c>[Required]</c> sozinho aceita <c>"   "</c>, e um título só de
/// espaços viraria manchete em branco no card e na página de detalhe.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotBlankAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is string texto && string.IsNullOrWhiteSpace(texto))
            return new ValidationResult(
                ErrorMessage ?? $"{context.MemberName} cannot be blank.",
                context.MemberName is null ? null : [context.MemberName]);

        return ValidationResult.Success;
    }
}
