using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using Portfolio.Validation;

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

    [Required(ErrorMessage = "Title is required.")]
    [NotBlank(ErrorMessage = "Title cannot be blank.")]
    [StringLength(120, ErrorMessage = "Title cannot exceed 120 characters.")]
    [FirestoreProperty("title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [NotBlank(ErrorMessage = "Description cannot be blank.")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    [FirestoreProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// As decisões de engenharia do projeto, na ordem em que devem ser lidas. Alimenta a página
    /// de detalhe (`/projects/{id}`); a home não as mostra.
    /// Vazio por padrão, nunca null: o Firestore é schemaless e os documentos gravados antes
    /// deste campo existir simplesmente não têm a chave, caso em que a leitura devolve lista
    /// vazia e quem consome faz `.map` direto, sem checar null.
    /// </summary>
    [MaxLength(10, ErrorMessage = "A project cannot have more than 10 decisions.")]
    [ValidateItems]
    [FirestoreProperty("decisions")]
    public Decision[] Decisions { get; set; } = [];

    [MaxLength(12, ErrorMessage = "A project cannot have more than 12 tags.")]
    [ValidateItems(MaxLength = 30)]
    [FirestoreProperty("tags")]
    public string[] Tags { get; set; } = [];

    [StringLength(2048, ErrorMessage = "GithubUrl cannot exceed 2048 characters.")]
    [RegularExpression(@"^https?://\S+$", ErrorMessage = "GithubUrl must be an http or https URL.")]
    [FirestoreProperty("githubUrl")]
    public string? GithubUrl { get; set; }

    [StringLength(2048, ErrorMessage = "LiveUrl cannot exceed 2048 characters.")]
    [RegularExpression(@"^https?://\S+$", ErrorMessage = "LiveUrl must be an http or https URL.")]
    [FirestoreProperty("liveUrl")]
    public string? LiveUrl { get; set; }

    [StringLength(2048, ErrorMessage = "ImageUrl cannot exceed 2048 characters.")]
    [RegularExpression(@"^https?://\S+$", ErrorMessage = "ImageUrl must be an http or https URL.")]
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
