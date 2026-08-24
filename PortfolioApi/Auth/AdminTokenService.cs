using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Portfolio.Auth;

/// <summary>
/// Emite o JWT do admin. Stateless de propósito: a instância do Render dorme e sobe zerada,
/// e uma sessão em memória exigiria login novo a cada religamento (design D-1).
/// </summary>
public class AdminTokenService(IOptions<AdminOptions> options)
{
    public const string Issuer = "portfolio-api";
    public const string Audience = "portfolio-admin";

    private readonly AdminOptions _options = options.Value;

    public (string Token, DateTimeOffset ExpiresAt) Issue()
    {
        var expiraEm = DateTime.UtcNow.AddHours(_options.TokenLifetimeHours);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")]),
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(SigningKey(_options), SecurityAlgorithms.HmacSha256),
        });

        return (token, new DateTimeOffset(expiraEm, TimeSpan.Zero));
    }

    /// <summary>
    /// Chave de assinatura. Sem configuração válida devolve uma chave aleatória do processo:
    /// ninguém consegue emitir (o login já recusa antes) e nenhum token de fora valida (ADM-04).
    /// </summary>
    public static SymmetricSecurityKey SigningKey(AdminOptions options) =>
        new(options.IsConfigured
            ? Encoding.UTF8.GetBytes(options.JwtSigningKey)
            : RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// <c>ClockSkew</c> zerado de propósito: o padrão da biblioteca é 5 minutos, e com ele um
    /// token expirado continua sendo aceito por esse tempo — verificado no protótipo (bloco A4).
    /// </summary>
    public static TokenValidationParameters ValidationParameters(AdminOptions options) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey(options),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };
}
