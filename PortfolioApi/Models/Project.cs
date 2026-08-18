using Google.Cloud.Firestore;

namespace Portfolio.Models;

[FirestoreData]
public class Project
{
    private DateTime _createdAt = DateTime.UtcNow;

    /// <summary>
    /// Id do documento no Firestore (string de 20 caracteres gerada pelo serviço).
    /// Preenchido na leitura; não é gravado como campo do documento.
    /// </summary>
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("title")]
    public string Title { get; set; } = string.Empty;

    [FirestoreProperty("description")]
    public string Description { get; set; } = string.Empty;

    [FirestoreProperty("tags")]
    public string[] Tags { get; set; } = [];

    [FirestoreProperty("githubUrl")]
    public string? GithubUrl { get; set; }

    [FirestoreProperty("liveUrl")]
    public string? LiveUrl { get; set; }

    [FirestoreProperty("imageUrl")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// O Firestore só aceita DateTime com Kind Utc — qualquer outro Kind lança
    /// ArgumentException na gravação. Normalizamos aqui para que chamadores possam
    /// atribuir DateTime.Now sem quebrar em runtime.
    /// </summary>
    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
