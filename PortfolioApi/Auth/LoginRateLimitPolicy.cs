using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Portfolio.Auth;

/// <summary>
/// Cinco tentativas de login por minuto, com <c>429</c> em vez do <c>503</c> global.
/// <c>RateLimiterOptions.RejectionStatusCode</c> vale para o limitador inteiro, e mudá-lo
/// alteraria também a resposta de <c>/api/contact</c> — política própria evita esse efeito
/// colateral (design D-10).
///
/// A partição é única, não por IP: atrás do proxy do Render o IP do cliente só chega em
/// <c>X-Forwarded-For</c>, cabeçalho que o atacante controla. Particionar por ele daria
/// isolamento aparente e nenhum real. O custo assumido é herdar o C-6: alguém pode esgotar a
/// janela e me deixar um minuto sem login.
/// </summary>
public class LoginRateLimitPolicy : IRateLimiterPolicy<string>
{
    public const string Name = "admin-login";

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        (context, _) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return ValueTask.CompletedTask;
        };

    public RateLimitPartition<string> GetPartition(HttpContext httpContext) =>
        RateLimitPartition.GetFixedWindowLimiter(Name, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        });
}
