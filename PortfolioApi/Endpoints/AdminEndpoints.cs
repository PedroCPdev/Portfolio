using Microsoft.Extensions.Options;
using Portfolio.Auth;
using Portfolio.Models;

namespace Portfolio.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin");

        group.MapPost("/login", (
            LoginRequest request,
            AdminTokenService tokens,
            IOptions<AdminOptions> options,
            ILogger<Program> logger) =>
        {
            var admin = options.Value;

            // Um único ponto de saída para "não autorizado": a resposta é byte a byte a mesma
            // para senha errada e para admin não configurado, para não contar a quem tenta
            // em qual dos dois estados o servidor está (ADM-02).
            if (!admin.IsConfigured)
            {
                logger.LogWarning(
                    "[Admin] Tentativa de login com a área administrativa não configurada. " +
                    "Defina Admin__PasswordHash e Admin__JwtSigningKey (>= 32 bytes).");
                return NaoAutorizado();
            }

            if (!AdminPasswordHasher.Verify(request.Password, admin.PasswordHash))
            {
                logger.LogWarning("[Admin] Tentativa de login com senha incorreta.");
                return NaoAutorizado();
            }

            var (token, expiresAt) = tokens.Issue();
            logger.LogInformation("[Admin] Login concedido até {ExpiresAt:o}.", expiresAt);
            return Results.Ok(new { token, expiresAt });
        })
        .RequireRateLimiting(LoginRateLimitPolicy.Name);
    }

    private static IResult NaoAutorizado() =>
        Results.Json(
            new { success = false, error = "Invalid credentials." },
            statusCode: StatusCodes.Status401Unauthorized);
}
