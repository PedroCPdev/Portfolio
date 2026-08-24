using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Portfolio.Tests;

/// <summary>
/// Sobe a API de verdade em memória para exercitar autenticação por HTTP.
///
/// O Firestore é apontado para um <c>ProjectId</c> que não é o de produção: a fábrica lê o
/// <c>appsettings.json</c> real da API (cujo ProjectId É o de produção), e sem essa troca o seed
/// do startup poderia escrever no banco real numa máquina com credencial válida. Diferente do
/// que o design do C-5 previa, **não** mexemos em <c>FIRESTORE_EMULATOR_HOST</c>: a variável é
/// global ao processo e derrubaria os testes de integração que rodam em paralelo (design D-16).
///
/// Uma fábrica por teste, nunca compartilhada: o rate limit do login é por aplicação, então uma
/// instância só faria o sexto teste receber 429 por motivo alheio ao que ele mede (C-5 → D-7).
/// </summary>
public class AdminApiFactory : WebApplicationFactory<Program>
{
    public const string SenhaCorreta = "senha-do-gate-2026";

    /// <summary>Hash de <see cref="SenhaCorreta"/>, gerado uma vez por fábrica.</summary>
    private readonly string _passwordHash;
    private readonly string _signingKey;
    private readonly Dictionary<string, string?> _extras;

    public AdminApiFactory(
        bool comSenha = true,
        string signingKey = "chave-de-teste-com-pelo-menos-32-bytes-de-verdade",
        Dictionary<string, string?>? extras = null)
    {
        _passwordHash = comSenha ? Portfolio.Auth.AdminPasswordHasher.Hash(SenhaCorreta) : string.Empty;
        _signingKey = signingKey;
        _extras = extras ?? [];
    }

    public string SigningKey => _signingKey;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Admin:PasswordHash", _passwordHash);
        builder.UseSetting("Admin:JwtSigningKey", _signingKey);
        builder.UseSetting("Firestore:ProjectId", "demo-portfolio-http");
        builder.UseSetting("Firestore:CredentialsJson", string.Empty);
        builder.UseSetting("Email:ApiKey", string.Empty);

        foreach (var (chave, valor) in _extras) builder.UseSetting(chave, valor);
    }
}
