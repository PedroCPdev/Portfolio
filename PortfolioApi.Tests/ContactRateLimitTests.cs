using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Portfolio.Tests;

/// <summary>
/// Rate limit do <c>POST /api/contact</c> por visitante (C-6), por HTTP contra a aplicação real.
///
/// Antes desta feature a janela de 3/min era uma partição única: três requisições de um script
/// deixavam o formulário indisponível para todo mundo, e o excedente recebia <c>503</c>.
///
/// Uma fábrica por teste, sempre: os limitadores vivem no processo da aplicação, então duas
/// classes de teste compartilhando fábrica se contaminariam pelo teto global.
/// </summary>
public class ContactRateLimitTests
{
    private const int PorVisitante = 3;
    private const int TetoDoEndpoint = 20;

    private static StringContent Mensagem() => new(
        JsonSerializer.Serialize(new
        {
            name = "Visitante",
            email = "visitante@exemplo.dev",
            message = "Olá, gostei do portfólio.",
        }),
        Encoding.UTF8,
        "application/json");

    /// <summary>Um POST em /api/contact anunciando <paramref name="xff"/> em X-Forwarded-For.</summary>
    private static async Task<HttpStatusCode> Enviar(HttpClient cliente, string? xff)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/contact") { Content = Mensagem() };
        if (xff is not null) requisicao.Headers.Add("X-Forwarded-For", xff);

        var resposta = await cliente.SendAsync(requisicao);
        return resposta.StatusCode;
    }

    private static async Task<List<HttpStatusCode>> EnviarVarias(HttpClient cliente, string? xff, int vezes)
    {
        var codigos = new List<HttpStatusCode>();
        for (var i = 0; i < vezes; i++) codigos.Add(await Enviar(cliente, xff));
        return codigos;
    }

    // RL-01: o excedente do próprio visitante é recusado com 429 (não 503) e não vira e-mail.
    [Fact]
    public async Task Quarta_requisicao_do_mesmo_visitante_recebe_429_e_nao_chama_o_resend()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);
        var cliente = api.CreateClient();

        var codigos = await EnviarVarias(cliente, "203.0.113.10", PorVisitante + 1);

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests],
            codigos);
        Assert.Equal(PorVisitante, espiao.Chamadas);
    }

    // RL-02: o coração do C-6 — um visitante esgotado não derruba o formulário para o outro.
    [Fact]
    public async Task Visitante_esgotado_nao_consome_a_cota_de_outro_visitante()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);
        var cliente = api.CreateClient();

        await EnviarVarias(cliente, "203.0.113.10", PorVisitante + 2);

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK],
            await EnviarVarias(cliente, "198.51.100.20", PorVisitante));
    }

    // RL-03: o teto do endpoint segura quem forja o cabeçalho — nem IP novo passa depois dele.
    [Fact]
    public async Task Acima_do_teto_do_endpoint_ate_um_ip_novo_recebe_429()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);
        var cliente = api.CreateClient();

        // 7 visitantes × 3 = 21 tentativas: as 20 primeiras cabem no teto, a 21ª não. O laço é
        // fechado de propósito — laço que só termina quando o teto é atingido trava para sempre
        // se a implementação nunca aceitar 20.
        var aceitas = 0;
        for (var visitante = 0; visitante < 7; visitante++)
        {
            foreach (var codigo in await EnviarVarias(cliente, $"203.0.113.{visitante}", PorVisitante))
                if (codigo == HttpStatusCode.OK) aceitas++;
        }

        Assert.Equal(TetoDoEndpoint, aceitas);
        Assert.Equal(TetoDoEndpoint, espiao.Chamadas);
        Assert.Equal(HttpStatusCode.TooManyRequests, await Enviar(cliente, "198.51.100.77"));
    }

    // RL-04: recusa por visitante NÃO pode consumir o teto — se consumisse (e consome, quando o
    // teto é adquirido antes da partição por IP), um atacante sozinho esgotaria o teto e o C-6
    // voltaria. Com o dobro de recusadas do teto inteiro, os visitantes seguintes têm de passar.
    [Fact]
    public async Task Recusa_do_proprio_visitante_nao_consome_o_teto_do_endpoint()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);
        var cliente = api.CreateClient();

        await EnviarVarias(cliente, "203.0.113.10", PorVisitante + (TetoDoEndpoint * 2));

        foreach (var visitante in new[] { "198.51.100.21", "198.51.100.22", "198.51.100.23" })
            Assert.Equal(
                [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK],
                await EnviarVarias(cliente, visitante, PorVisitante));
    }

    // RL-05: a partição é o endereço mais à direita, o que o proxy anexou. Se fosse o da
    // esquerda — o que o cliente escreve — cada requisição abaixo cairia numa partição própria
    // e nenhuma seria recusada.
    [Fact]
    public async Task Particao_usa_o_ip_mais_a_direita_do_x_forwarded_for()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);
        var cliente = api.CreateClient();

        var codigos = new List<HttpStatusCode>();
        for (var forjado = 0; forjado < PorVisitante + 1; forjado++)
            codigos.Add(await Enviar(cliente, $"10.0.0.{forjado}, 203.0.113.30"));

        Assert.Equal(HttpStatusCode.TooManyRequests, codigos[^1]);
        Assert.Equal(PorVisitante, espiao.Chamadas);
    }

    // RL-06: sem cabeçalho (ou com lixo) o limite continua existindo — degrada para uma partição
    // só, que é o comportamento de hoje, nunca para "sem limite".
    [Theory]
    [InlineData(null)]
    [InlineData("nao-e-um-ip")]
    public async Task Sem_x_forwarded_for_valido_o_limite_continua_valendo(string? xff)
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);
        var cliente = api.CreateClient();

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests],
            await EnviarVarias(cliente, xff, PorVisitante + 1));
    }

    // RL-07: o limitador é do contato. /health é o alvo do keep-alive (~96 pings/dia) e não pode
    // herdar teto nenhum.
    [Fact]
    public async Task Health_nao_e_afetado_pelo_limitador_do_contato()
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);
        var cliente = api.CreateClient();

        for (var i = 0; i < TetoDoEndpoint + 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/health")).StatusCode);

        Assert.Equal(0, espiao.Chamadas);
    }
}
