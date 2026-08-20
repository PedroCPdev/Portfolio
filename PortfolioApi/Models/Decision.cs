using Google.Cloud.Firestore;

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
    [FirestoreProperty("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Por que essa saída foi escolhida — o problema que ela resolve.</summary>
    [FirestoreProperty("why")]
    public string Why { get; set; } = string.Empty;

    /// <summary>
    /// O que a decisão custou. Opcional: uma decisão sem custo declarado ainda é exibida,
    /// só sem a linha de custo.
    /// </summary>
    [FirestoreProperty("cost")]
    public string Cost { get; set; } = string.Empty;
}
