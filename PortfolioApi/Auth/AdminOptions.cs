namespace Portfolio.Auth;

/// <summary>
/// Seção <c>Admin</c> da configuração. No Render vem como <c>Admin__PasswordHash</c> e
/// <c>Admin__JwtSigningKey</c>; local, do <c>appsettings.json</c> (gitignored).
/// </summary>
public class AdminOptions
{
    /// <summary>Saída de <c>dotnet run -- hash-password &lt;senha&gt;</c>. Nunca a senha em texto puro.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Segredo de assinatura do JWT. HMAC-SHA256 exige pelo menos 32 bytes.</summary>
    public string JwtSigningKey { get; set; } = string.Empty;

    public int TokenLifetimeHours { get; set; } = 8;

    /// <summary>
    /// Sem configuração completa a área administrativa fica fechada — login sempre 401 e nenhum
    /// token válido (ADM-04). É o oposto deliberado do <c>EmailService</c>, que degrada em
    /// silêncio: ali o pior caso é um e-mail perdido, aqui seria escrita anônima no banco.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PasswordHash) &&
        !string.IsNullOrWhiteSpace(JwtSigningKey) &&
        System.Text.Encoding.UTF8.GetByteCount(JwtSigningKey) >= 32;
}
