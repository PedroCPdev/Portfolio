using Portfolio.Models;

namespace Portfolio.Endpoints;

public static class ContactEndpoints
{
    public static void MapContactEndpoints(this WebApplication app)
    {
        app.MapPost("/api/contact", (ContactMessage message) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(message.Email) ||
                        string.IsNullOrWhiteSpace(message.Message))
                        return Task.FromResult(Results.BadRequest("Email and Message required."));

                    Console.WriteLine($"[Contact] From: {message.Name} <{message.Email}>");
                    Console.WriteLine($"Message: {message.Message}");

                    return Task.FromResult(Results.Ok(new { success = true, message = "Message sent!" }));
                }
                catch (Exception exception)
                {
                    return Task.FromException<IResult>(exception);
                }
            })
            .WithTags("Contact");
    }
}
