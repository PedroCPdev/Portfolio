using Microsoft.AspNetCore.Mvc;
using Portfolio.Models;
using Portfolio.Services;
using Portfolio.Validation;

namespace Portfolio.Endpoints;

public static class ContactEndpoints
{
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
        app.MapPost("/api/contact", async (ContactMessage message, EmailService emailService, ILogger<Program> logger) =>
            {
                logger.LogInformation("[Contact] From: {Name} <{Email}>", message.Name, message.Email);

                try
                {
                    await emailService.SendContactMessageAsync(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send contact email from {Email}", message.Email);
                    return Results.Json(
                        new { success = false, error = "Failed to send message. Try again later." },
                        statusCode: StatusCodes.Status502BadGateway);
                }

                return Results.Ok(new { success = true, message = "Message sent!" });
            })
            .WithTags("Contact")
            .AddEndpointFilter<ValidationFilter<ContactMessage>>()
            .WithMetadata(new RequestSizeLimitAttribute(LimiteDoCorpoEmBytes))
            .RequireRateLimiting("contact");
    }
}
