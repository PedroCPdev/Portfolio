namespace Portfolio.Data;

/// <summary>
/// Configuração da conexão com o Cloud Firestore (seção "Firestore").
/// No Render, use as variáveis de ambiente Firestore__ProjectId e Firestore__CredentialsJson.
/// </summary>
public class FirestoreOptions
{
    /// <summary>Id do projeto Firebase/GCP. Ex.: "meu-portfolio-1a2b3".</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Conteúdo integral do JSON da service account. Fica em variável de ambiente para
    /// não exigir arquivo de credencial em disco no Render. Quando vazio, cai para as
    /// Application Default Credentials (dev local com gcloud, ou emulador).
    /// </summary>
    public string CredentialsJson { get; set; } = string.Empty;

    /// <summary>Nome da coleção dos projetos.</summary>
    public string ProjectsCollection { get; set; } = "projects";
}
