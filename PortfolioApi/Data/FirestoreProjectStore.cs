using Google.Cloud.Firestore;
using Grpc.Core;
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

    /// <summary>Retorna null quando o documento não existe ou o id não é válido — não lança.</summary>
    public async Task<Project?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!IdValido(id)) return null;

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

    /// <summary>
    /// Sobrescreve o projeto preservando o <c>CreatedAt</c> já gravado. Devolve false — sem lançar —
    /// quando o id não existe ou não é válido.
    ///
    /// Lê antes de gravar porque <c>SetAsync</c> substitui o documento inteiro: os campos ausentes
    /// no objeto voltariam a vazio, inclusive a data de criação (verificado no emulador, F3).
    /// </summary>
    public async Task<bool> UpdateAsync(string id, Project project, CancellationToken cancellationToken = default)
    {
        if (!IdValido(id)) return false;

        var documento = Projects.Document(id);
        var atual = await documento.GetSnapshotAsync(cancellationToken);
        if (!atual.Exists) return false;

        project.CreatedAt = atual.ConvertTo<Project>().CreatedAt;
        await documento.SetAsync(project, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>
    /// Remove o projeto. Devolve false quando o id não existe.
    ///
    /// A precondição não é zelo: <c>DeleteAsync</c> sem ela **não lança** para documento
    /// inexistente (verificado no emulador, F2), e o endpoint responderia 204 para um id
    /// inventado em vez de 404.
    /// </summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!IdValido(id)) return false;

        try
        {
            await Projects.Document(id).DeleteAsync(Precondition.MustExist, cancellationToken);
            return true;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            // gRPC não passa daqui: quem chama lida com bool, não com exceção de transporte.
            return false;
        }
    }

    /// <summary>
    /// <c>Document("a/b")</c> lança <c>ArgumentException</c> (verificado, F5) — um id com barra
    /// vindo da rota viraria 500. Com número par de barras ("a/b/c") ele não lança, mas lê um
    /// documento de subcoleção fora da lista de projetos. Aqui os dois são um id que não existe.
    /// </summary>
    private static bool IdValido(string id) =>
        !string.IsNullOrWhiteSpace(id) && !id.Contains('/');
}
