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

    /// <summary>
    /// As decisões de engenharia do projeto, na ordem em que devem ser lidas. Alimenta a página
    /// de detalhe (`/projects/{id}`); a home não as mostra.
    /// Vazio por padrão, nunca null: o Firestore é schemaless e os documentos gravados antes
    /// deste campo existir simplesmente não têm a chave, caso em que a leitura devolve lista
    /// vazia e quem consome faz `.map` direto, sem checar null.
    /// </summary>
    [FirestoreProperty("decisions")]
    public Decision[] Decisions { get; set; } = [];

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
