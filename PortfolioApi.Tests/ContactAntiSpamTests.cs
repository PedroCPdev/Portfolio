using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Portfolio.Tests;

/// <summary>
/// Honeypot e teto diário do <c>POST /api/contact</c> (C-10), por HTTP contra a aplicação real
/// com o Resend trocado pelo espião. Cada teste faz no máximo 2 requisições, abaixo dos 3/min por
/// visitante do C-6, para que um 429 nunca seja confundido com o que o teste mede.
/// </summary>
public class ContactAntiSpamTests
{
    private static StringContent Json(object corpo) =>
        new(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

    private static object Mensagem(string? website = null) => new
    {
        name = "Visitante",
        email = "visitante@exemplo.dev",
        message = "Olá, gostei do portfólio.",
        website,
    };

    // AS-01
    [Fact]
    public async Task Honeypot_preenchido_responde_200_sem_chamar_o_resend()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);

        var resposta = await api.CreateClient().PostAsync("/api/contact", Json(Mensagem(website: "https://spam.example")));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(corpo.GetProperty("success").GetBoolean());
        Assert.Equal(0, espiao.Chamadas);
    }

    // AS-05: com teto 1, se o honeypot consumisse o teto a mensagem legítima seguinte receberia 503.
    [Fact]
    public async Task Honeypot_nao_consome_o_teto()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao, limiteDiario: 1);
        var client = api.CreateClient();

        await client.PostAsync("/api/contact", Json(Mensagem(website: "https://spam.example")));
        var legitima = await client.PostAsync("/api/contact", Json(Mensagem()));

        Assert.Equal(HttpStatusCode.OK, legitima.StatusCode);
        Assert.Equal(1, espiao.Chamadas);
    }

    // AS-03
    [Fact]
    public async Task Teto_diario_atingido_responde_503_sem_chamar_o_resend()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao, limiteDiario: 1);
        var client = api.CreateClient();

        var primeira = await client.PostAsync("/api/contact", Json(Mensagem()));
        var segunda = await client.PostAsync("/api/contact", Json(Mensagem()));

        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, segunda.StatusCode);
        var corpo = await segunda.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(corpo.GetProperty("success").GetBoolean());
        Assert.Contains("GitHub", corpo.GetProperty("error").GetString());
        Assert.Equal(1, espiao.Chamadas);
    }
}
