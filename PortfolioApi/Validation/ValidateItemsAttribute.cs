using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Portfolio.Validation;

/// <summary>
/// Valida cada item da coleção. Existe porque <c>Validator.TryValidateObject</c> **não** desce em
/// item de coleção nem em objeto aninhado: verificado rodando (Cadeia de Verificação, bloco C),
/// um projeto com decisão de título vazio e tag de 100 caracteres passava como válido.
///
/// Item string é medido por <see cref="MaxLength"/> e recusado se vier em branco; qualquer outro
/// tipo é validado pelos próprios atributos dele.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateItemsAttribute : ValidationAttribute
{
    /// <summary>Tamanho máximo de cada item quando a coleção é de strings. Zero desliga a checagem.</summary>
    public int MaxLength { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not IEnumerable itens) return ValidationResult.Success;

        var campo = context.MemberName;
        string[]? membro = campo is null ? null : [campo];
        var posicao = 0;

        foreach (var item in itens)
        {
            var onde = $"{campo}[{posicao++}]";

            if (item is null)
                return new ValidationResult($"{onde} cannot be null.", membro);

            if (item is string texto)
            {
                if (string.IsNullOrWhiteSpace(texto))
                    return new ValidationResult($"{onde} cannot be blank.", membro);
                if (MaxLength > 0 && texto.Length > MaxLength)
                    return new ValidationResult($"{onde} cannot exceed {MaxLength} characters.", membro);
                continue;
            }

            var resultados = new List<ValidationResult>();
            if (!Validator.TryValidateObject(item, new ValidationContext(item), resultados, validateAllProperties: true))
                return new ValidationResult(
                    $"{onde}: {string.Join(" ", resultados.Select(r => r.ErrorMessage))}", membro);
        }

        return ValidationResult.Success;
    }
}
