using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Portfolio.Tests;

/// <summary>
/// Aplicação real com o e-mail configurado e o handler HTTP trocado pelo espião, para que
/// "não chamou o Resend" seja observação e não presunção. Herda de <see cref="AdminApiFactory"/>
/// pelo Firestore apontado para um <c>ProjectId</c> que não é o de produção.
///
/// Mora em arquivo próprio desde o C-6: <c>ContactEndpointTests</c> (validação de entrada) e
/// <c>ContactRateLimitTests</c> (rate limit) sobem a mesma aplicação, e duplicar a fábrica
/// deixaria duas configurações de teste divergindo em silêncio.
/// </summary>
/// <c>emailConfigurado: false</c> sobe a aplicação sem a seção <c>Email</c>, como produção ficaria
/// se a env var sumisse (C-9).
public class ContactApiFactory(ResendSpy espiao, bool emailConfigurado = true) : AdminApiFactory(
    extras: emailConfigurado
        ? new Dictionary<string, string?>
        {
            ["Email:ApiKey"] = "chave-de-teste-do-gate",
            ["Email:ToAddress"] = "destino@teste.dev",
        }
        : new Dictionary<string, string?>())
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.ConfigureHttpClientDefaults(b => b.ConfigurePrimaryHttpMessageHandler(() => espiao)));
    }
}

/// <summary>Intercepta a saída HTTP: conta as chamadas e nunca deixa o teste tocar a rede.</summary>
public sealed class ResendSpy : HttpMessageHandler
{
    public int Chamadas { get; private set; }
    public string? UltimaUrl { get; private set; }
    public string UltimoCorpo { get; private set; } = string.Empty;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Chamadas++;
        UltimaUrl = request.RequestUri?.ToString();
        UltimoCorpo = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":"fake-do-espiao"}""", Encoding.UTF8, "application/json"),
        };
    }
}
