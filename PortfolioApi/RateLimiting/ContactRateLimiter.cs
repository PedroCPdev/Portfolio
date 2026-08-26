using System.Threading.RateLimiting;
using Portfolio.Endpoints;

namespace Portfolio.RateLimiting;

/// <summary>
/// Limite de <c>POST /api/contact</c> em duas camadas encadeadas: a janela do visitante e um teto
/// para o endpoint inteiro (C-6).
///
/// Antes, uma partição única fazia os 3 req/min valerem para todos os visitantes somados — três
/// requisições de um script deixavam o formulário indisponível para o resto do mundo.
///
/// <para><b>A ordem do encadeamento é a decisão inteira.</b> Com o teto registrado em
/// <c>RateLimiterOptions.GlobalLimiter</c> e a janela do visitante como política do endpoint, o
/// middleware adquire o teto <i>primeiro</i>, e uma requisição recusada depois pela janela do
/// visitante consome <b>2</b> permits do teto (medido: design D-2 e evidências P9/P10). Um
/// atacante sozinho esgotaria o teto e reproduziria o C-6. Encadeando à mão, o teto só é
/// consultado por quem passou pela própria janela.</para>
///
/// <para>A partição é o <see cref="Microsoft.AspNetCore.Http.ConnectionInfo.RemoteIpAddress"/>,
/// que atrás do Render vem do <c>X-Forwarded-For</c> — ver a configuração de
/// <c>ForwardedHeaders</c> em <c>Program.cs</c>, sem a qual isto degrada para uma partição só.</para>
/// </summary>
public static class ContactRateLimiter
{
    public const int PorVisitantePorMinuto = 3;

    /// <summary>
    /// Teto do endpoint somando todos os visitantes. Existe porque quem alcançar a aplicação sem
    /// passar pelo proxy pode escrever o <c>X-Forwarded-For</c> que quiser e ganhar uma partição
    /// nova a cada requisição; o teto é o que transforma isso em 20/min em vez de infinito.
    /// Cabem 6 visitantes legítimos com a cota cheia ao mesmo tempo.
    /// </summary>
    public const int TetoDoEndpointPorMinuto = 20;

    /// <summary>Partição de quem chega sem IP conhecido — todos juntos, nunca liberados.</summary>
    private const string VisitanteSemIp = "sem-ip";

    private const string ForaDoContato = "fora-do-contato";

    /// <summary>
    /// Registrado como <c>GlobalLimiter</c>: roda para toda requisição, e por isso cada camada
    /// devolve <see cref="RateLimitPartition.GetNoLimiter{TKey}"/> para tudo que não é o contato —
    /// <c>/health</c> (alvo do keep-alive, ~96 pings/dia), <c>/api/projects</c> e o login seguem
    /// sem qualquer limite vindo daqui.
    /// </summary>
    public static PartitionedRateLimiter<HttpContext> Criar() =>
        PartitionedRateLimiter.CreateChained(JanelaDoVisitante(), TetoDoEndpoint());

    private static PartitionedRateLimiter<HttpContext> JanelaDoVisitante() =>
        PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
            EhOContato(contexto)
                ? RateLimitPartition.GetFixedWindowLimiter(
                    contexto.Connection.RemoteIpAddress?.ToString() ?? VisitanteSemIp,
                    _ => Janela(PorVisitantePorMinuto))
                : RateLimitPartition.GetNoLimiter<string>(ForaDoContato));

    private static PartitionedRateLimiter<HttpContext> TetoDoEndpoint() =>
        PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
            EhOContato(contexto)
                ? RateLimitPartition.GetFixedWindowLimiter(
                    ContactEndpoints.Rota,
                    _ => Janela(TetoDoEndpointPorMinuto))
                : RateLimitPartition.GetNoLimiter<string>(ForaDoContato));

    private static bool EhOContato(HttpContext contexto) =>
        contexto.Request.Path.Equals(ContactEndpoints.Rota, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <c>QueueLimit = 0</c> é o que faz o excedente ser recusado em vez de enfileirado — mesmo
    /// formato de <c>Auth/LoginRateLimitPolicy.cs</c>.
    /// </summary>
    private static FixedWindowRateLimiterOptions Janela(int permissoes) => new()
    {
        PermitLimit = permissoes,
        Window = TimeSpan.FromMinutes(1),
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0,
    };
}
