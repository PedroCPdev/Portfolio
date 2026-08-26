using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Portfolio.Auth;

/// <summary>
/// Cinco tentativas de login por minuto.
///
/// O <c>OnRejected</c> daqui nasceu (D-10) para dar <c>429</c> ao login sem mudar o <c>503</c>
/// que o contato então devolvia. Desde o C-6 o <c>RejectionStatusCode</c> global já é <c>429</c>,
/// então ele virou redundante — mantido por ser redundante, não errado, e por remover código de
/// autenticação estar fora do escopo daquela tarefa.
///
/// A partição é única, não por IP. O motivo original registrado — "o <c>X-Forwarded-For</c> é
/// controlado pelo atacante" — vale para o endereço mais à ESQUERDA do cabeçalho; o C-6 mediu
/// que o middleware lê o mais à direita, anexado pelo proxy, e passou a particionar o contato
/// por ele. Aqui nada mudou: o custo assumido segue sendo alguém esgotar a janela e me deixar um
/// minuto sem login. Estender o mesmo tratamento ao login é ideia adiada, não dívida escondida.
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
