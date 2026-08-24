using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using Portfolio.Validation;

namespace Portfolio.Models;

/// <summary>
/// Uma decisão de engenharia tomada num projeto, com o motivo e o preço.
/// A ordem dentro de <see cref="Project.Decisions"/> é informação: é a sequência em que o
/// autor quer que sejam lidas, não um detalhe de armazenamento.
/// </summary>
[FirestoreData]
public class Decision
{
    /// <summary>O que foi decidido. Ex.: "Firestore over PostgreSQL".</summary>
    [Required(ErrorMessage = "Decision title is required.")]
    [NotBlank(ErrorMessage = "Decision title cannot be blank.")]
    [StringLength(120, ErrorMessage = "Decision title cannot exceed 120 characters.")]
    [FirestoreProperty("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Por que essa saída foi escolhida — o problema que ela resolve.</summary>
    [Required(ErrorMessage = "Decision why is required.")]
    [NotBlank(ErrorMessage = "Decision why cannot be blank.")]
    [StringLength(1500, ErrorMessage = "Decision why cannot exceed 1500 characters.")]
    [FirestoreProperty("why")]
    public string Why { get; set; } = string.Empty;

    /// <summary>
    /// O que a decisão custou. Opcional: uma decisão sem custo declarado ainda é exibida,
    /// só sem a linha de custo.
    /// </summary>
    [StringLength(1500, ErrorMessage = "Decision cost cannot exceed 1500 characters.")]
    [FirestoreProperty("cost")]
    public string Cost { get; set; } = string.Empty;
}
