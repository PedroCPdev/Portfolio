using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;
using Portfolio.Models;

namespace Portfolio.Data;

/// <summary>
/// Único ponto do sistema que fala Firestore. Ocupa o lugar que o AppDbContext ocupava:
/// uma peça de acesso a dados injetada diretamente nos endpoints.
/// </summary>
public class FirestoreProjectStore(FirestoreDb db, IOptions<FirestoreOptions> options)
{
    private readonly string _collection = string.IsNullOrWhiteSpace(options.Value.ProjectsCollection)
        ? "projects"
        : options.Value.ProjectsCollection;

    private CollectionReference Projects => db.Collection(_collection);

    /// <summary>Projetos ordenados do mais recente para o mais antigo.</summary>
    public async Task<List<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await Projects
            .OrderByDescending("createdAt")
            .GetSnapshotAsync(cancellationToken);

        return snapshot.Documents.Select(doc => doc.ConvertTo<Project>()).ToList();
    }

    /// <summary>Retorna null quando o documento não existe — não lança.</summary>
    public async Task<Project?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var snapshot = await Projects.Document(id).GetSnapshotAsync(cancellationToken);
        return snapshot.Exists ? snapshot.ConvertTo<Project>() : null;
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await Projects.Count().GetSnapshotAsync(cancellationToken);
        return snapshot.Count ?? 0;
    }

    public async Task<string> AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        var reference = await Projects.AddAsync(project, cancellationToken);
        return reference.Id;
    }
}
