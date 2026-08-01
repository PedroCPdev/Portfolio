using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Portfolio.Models;

namespace Portfolio.Services;

public class EmailOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "onboarding@resend.dev";
    public string ToAddress { get; set; } = string.Empty;
}

public class EmailService(IOptions<EmailOptions> options, IHttpClientFactory httpClientFactory, ILogger<EmailService> logger)
{
    private readonly EmailOptions _options = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.ToAddress);

    public async Task SendContactMessageAsync(ContactMessage message)
    {
        if (!IsConfigured)
        {
            logger.LogWarning(
                "Email section not configured; contact message from {Email} was logged but not emailed",
                message.Email);
            return;
        }

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.resend.com/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var response = await client.PostAsJsonAsync("emails", new
        {
            from = $"Portfolio Contact <{_options.FromAddress}>",
            to = new[] { _options.ToAddress },
            subject = $"New contact message from {message.Name}",
            text = $"From: {message.Name} <{message.Email}>\n\n{message.Message}",
            reply_to = message.Email,
        });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend API returned {(int)response.StatusCode}: {body}");
        }
    }
}
