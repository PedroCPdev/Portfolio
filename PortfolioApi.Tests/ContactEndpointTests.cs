using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Portfolio.Tests;

/// <summary>
/// Validação de entrada do <c>POST /api/contact</c> (C-5), por HTTP contra a aplicação real.
///
/// O que torna estes testes capazes de falhar: o e-mail é **configurado** e o handler HTTP é
/// trocado por um espião (design D-8). Com <c>Email:ApiKey</c> vazio o EmailService degrada em
/// silêncio e nunca chamaria nada — um teste nesse estado afirmaria "não chamou o Resend" sem
/// poder falhar. Aqui, o caminho feliz chama o espião e o inválido não chega nele.
///
/// Uma fábrica por teste (D-7): desde o C-6 o limite é de 3/min por visitante, e no TestServer
/// todos estes testes caem na mesma partição (não há X-Forwarded-For e a conexão em memória não
/// tem IP), então uma fábrica compartilhada faria o quarto teste receber 429 por motivo alheio
/// ao que ele mede. Nenhum destes testes exige o emulador do Firestore.
///
/// A fábrica e o espião moram em ContactApiFactory.cs — ContactRateLimitTests usa os mesmos.
/// </summary>
public class ContactEndpointTests
{
    private static StringContent Json(object corpo) =>
        new(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

    private static object Mensagem(string? name = "Visitante", string? email = "visitante@exemplo.dev",
        string? message = "Olá, gostei do portfólio.") => new { name, email, message };

    private static async Task<(HttpResponseMessage Resposta, ResendSpy Espiao)> Enviar(object corpo)
    {
        var espiao = new ResendSpy();
        using var api = new ContactApiFactory(espiao);
        var resposta = await api.CreateClient().PostAsync("/api/contact", Json(corpo));
        return (resposta, espiao);
    }

    private static async Task AssertRecusado(HttpResponseMessage resposta, ResendSpy espiao)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(corpo.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("error").GetString()));

        Assert.Equal(0, espiao.Chamadas);
    }

    // CT-01 (falha): campo ausente, nulo ou só espaços não passa e não vira e-mail.
    [Theory]
    [InlineData(null, "visitante@exemplo.dev", "mensagem")]   // Name ausente
    [InlineData("Visitante", null, "mensagem")]               // Email ausente
    [InlineData("Visitante", "visitante@exemplo.dev", null)]  // Message ausente
    [InlineData("   ", "visitante@exemplo.dev", "mensagem")]  // Name só de espaços
    [InlineData("Visitante", "   ", "mensagem")]              // Email só de espaços
    [InlineData("Visitante", "visitante@exemplo.dev", "  ")]  // Message só de espaços
    public async Task Campo_ausente_ou_em_branco_devolve_400_e_nao_chama_o_resend(
        string? name, string? email, string? message)
    {
        var (resposta, espiao) = await Enviar(Mensagem(name, email, message));
        await AssertRecusado(resposta, espiao);
    }

    // CT-02 (falha): formato de e-mail. O segundo caso é tentativa de injeção de cabeçalho —
    // o Resend recebe JSON, então CRLF já não era vetor, mas a porta fecha antes.
    [Theory]
    [InlineData("nao-e-um-email")]
    [InlineData("visitante@exemplo.dev\nBcc: outro@exemplo.dev")]
    [InlineData("@exemplo.dev")]
    public async Task Email_malformado_devolve_400_e_nao_chama_o_resend(string email)
    {
        var (resposta, espiao) = await Enviar(Mensagem(email: email));
        await AssertRecusado(resposta, espiao);
    }

    // CT-03 (falha): limites de tamanho — 80 / 254 / 4000.
    [Fact]
    public async Task Name_acima_de_80_caracteres_devolve_400_e_nao_chama_o_resend()
    {
        var (resposta, espiao) = await Enviar(Mensagem(name: new string('a', 81)));
        await AssertRecusado(resposta, espiao);
    }

    [Fact]
    public async Task Email_acima_de_254_caracteres_devolve_400_e_nao_chama_o_resend()
    {
        // 243 + "@exemplo.dev" (12) = 255 caracteres, e ainda assim um endereço bem formado:
        // o que reprova aqui é o comprimento, não o formato.
        var (resposta, espiao) = await Enviar(Mensagem(email: new string('a', 243) + "@exemplo.dev"));
        await AssertRecusado(resposta, espiao);
    }

    [Fact]
    public async Task Message_acima_de_4000_caracteres_devolve_400_e_nao_chama_o_resend()
    {
        var (resposta, espiao) = await Enviar(Mensagem(message: new string('a', 4001)));
        await AssertRecusado(resposta, espiao);
    }

    // CT-03 (limite exato): 80 / 254 / 4000 são aceitos — a barreira está no limite, não antes.
    [Fact]
    public async Task Campos_exatamente_no_limite_sao_aceitos()
    {
        var (resposta, espiao) = await Enviar(Mensagem(
            name: new string('a', 80),
            email: new string('a', 242) + "@exemplo.dev",
            message: new string('a', 4000)));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(1, espiao.Chamadas);
    }

    // CT-04: o endpoint declara o teto de 32 KB para o corpo.
    //
    // Por que metadata e não uma requisição de 1 MB: o TestServer em memória não aplica
    // IHttpMaxRequestBodySizeFeature (L-009), então o corpo chega inteiro e quem responde é a
    // validação (400), mascarando o mecanismo. Contra Kestrel real o mesmo corpo devolve 413 —
    // verificado e registrado em evidencias/20260826-limite-de-corpo-contato-kestrel.md.
    [Fact]
    public void Endpoint_de_contato_declara_o_limite_de_corpo_de_32_kb()
    {
        using var api = new ContactApiFactory(new ResendSpy());
        var fonte = api.Services.GetRequiredService<EndpointDataSource>();

        var contato = Assert.Single(
            fonte.Endpoints.OfType<RouteEndpoint>(),
            e => e.RoutePattern.RawText == "/api/contact");

        Assert.Equal(32 * 1024, contato.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize);
    }

    // CT-05 (sucesso): o caminho válido segue igual ao de antes — e agora dá para provar que
    // ele de fato chega ao Resend, com o conteúdo do formulário.
    [Fact]
    public async Task Mensagem_valida_devolve_200_e_chama_o_resend_uma_vez()
    {
        var (resposta, espiao) = await Enviar(Mensagem());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(corpo.GetProperty("success").GetBoolean());

        Assert.Equal(1, espiao.Chamadas);
        Assert.Equal("https://api.resend.com/emails", espiao.UltimaUrl);
        Assert.Contains("visitante@exemplo.dev", espiao.UltimoCorpo);
    }
}
