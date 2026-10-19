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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ToAddress);
}

public class EmailService(IOptions<EmailOptions> options, IHttpClientFactory httpClientFactory)
{
    private readonly EmailOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task SendContactMessageAsync(ContactMessage message)
    {
        // O endpoint checa IsConfigured antes e responde 503 (C-9). Chegar aqui sem config é bug de
        // quem chama: lançar evita a volta do sucesso falso, em que a mensagem sumia.
        if (!IsConfigured)
            throw new InvalidOperationException("Email section not configured; nothing was sent.");

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
