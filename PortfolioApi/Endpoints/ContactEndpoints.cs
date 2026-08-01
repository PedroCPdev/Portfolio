using Portfolio.Models;
using Portfolio.Services;

namespace Portfolio.Endpoints;

public static class ContactEndpoints
{
    public static void MapContactEndpoints(this WebApplication app)
    {
        app.MapPost("/api/contact", async (ContactMessage message, EmailService emailService, ILogger<Program> logger) =>
            {
                if (string.IsNullOrWhiteSpace(message.Email) ||
                    string.IsNullOrWhiteSpace(message.Message))
                    return Results.BadRequest(new { success = false, error = "Email and Message required." });

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
            .RequireRateLimiting("contact");
    }
}
