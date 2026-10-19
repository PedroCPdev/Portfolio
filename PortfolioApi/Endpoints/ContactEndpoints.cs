using Microsoft.AspNetCore.Mvc;
using Portfolio.Models;
using Portfolio.Services;
using Portfolio.Validation;

namespace Portfolio.Endpoints;

public static class ContactEndpoints
{
    /// <summary>
    /// Rota do endpoint. É constante porque o limitador do C-6
    /// (<c>RateLimiting/ContactRateLimiter.cs</c>) reconhece a requisição pelo caminho — renomear
    /// a rota com o caminho escrito à mão nos dois lugares desligaria o rate limit em silêncio.
    /// </summary>
    public const string Rota = "/api/contact";

    /// <summary>
    /// Pior caso legítimo é ~16 KB (mensagem de 4000 chars em UTF-8 + os outros campos). O dobro
    /// de folga, e ainda 1000× menor que os 30 MB que o Kestrel aceita por padrão. Metadata do
    /// endpoint, não limite global no Kestrel: um teto global valeria para qualquer endpoint
    /// futuro sem que quem o escrevesse percebesse (design D-3).
    /// </summary>
    private const int LimiteDoCorpoEmBytes = 32 * 1024;

    public static void MapContactEndpoints(this WebApplication app)
    {
        // A validação do corpo é do ValidationFilter, não daqui: os limites vivem declarados em
        // ContactMessage, que é onde se lê o contrato. Antes, a única checagem era um
        // IsNullOrWhiteSpace de Email e Message escrito neste handler — o resto passava.
        // O rate limit não aparece aqui: desde o C-6 são duas camadas encadeadas (3/min por
        // visitante + 20/min no endpoint), e a ordem de aquisição só é expressável fora da
        // política de endpoint — ver RateLimiting/ContactRateLimiter.cs e o design D-2.
        app.MapPost(Rota, async (ContactMessage message, EmailService emailService, ILogger<Program> logger) =>
            {
                logger.LogInformation("[Contact] Mensagem recebida ({Length} caracteres)", message.Message.Length);

                // Sem a seção Email a API respondia sucesso e a mensagem sumia (C-9). O 503 leva o
                // visitante para os outros contatos da página.
                if (!emailService.IsConfigured)
                {
                    logger.LogError("[Contact] Email não configurado: mensagem recusada com 503");
                    return Results.Json(
                        new { success = false, error = "The contact form is unavailable right now. Reach me on GitHub or LinkedIn." },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                try
                {
                    await emailService.SendContactMessageAsync(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send contact email");
                    return Results.Json(
                        new { success = false, error = "Failed to send message. Try again later." },
                        statusCode: StatusCodes.Status502BadGateway);
                }

                return Results.Ok(new { success = true, message = "Message sent!" });
            })
            .WithTags("Contact")
            .AddEndpointFilter<ValidationFilter<ContactMessage>>()
            .WithMetadata(new RequestSizeLimitAttribute(LimiteDoCorpoEmBytes));
    }
}
