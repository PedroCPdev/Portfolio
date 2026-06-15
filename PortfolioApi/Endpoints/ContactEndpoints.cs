using Portfolio.Models;

namespace Portfolio.Endpoints;

public static class ContactEndpoints
{
    public static void MapContactEndpoints(this WebApplication app)
    {
        app.MapPost("/api/contact", async (ContactMessage message) =>
            {
                if (string.IsNullOrWhiteSpace(message.Email) ||
                    string.IsNullOrWhiteSpace(message.Message))
                    return Results.BadRequest("Email and Message required.");

                Console.WriteLine($"[Contact] From: {message.Name} <{message.Email}>");
                Console.WriteLine($"Message: {message.Message}");

                return Results.Ok(new { success = true, message = "Message sent!" });
            })
            .WithTags("Contact");
    }
}
