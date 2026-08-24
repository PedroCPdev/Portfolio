using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Xunit;

namespace Portfolio.Tests;

public class AdminAuthTests
{
    private static StringContent Corpo(string senha) =>
        new(JsonSerializer.Serialize(new { password = senha }), Encoding.UTF8, "application/json");

    // ADM-01: a senha correta devolve um token que valida contra a chave configurada
    [Fact]
    public async Task Login_com_senha_correta_devolve_token_assinado_e_validade()
    {
        using var app = new AdminApiFactory();
        using var client = app.CreateClient();

        var response = await client.PostAsync("/api/admin/login", Corpo(AdminApiFactory.SenhaCorreta));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = corpo.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "portfolio-api",
            ValidAudience = "portfolio-admin",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(app.SigningKey)),
            ClockSkew = TimeSpan.Zero,
        });
        Assert.True(resultado.IsValid);
        Assert.True(corpo.GetProperty("expiresAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    // ADM-02: senha errada não abre e não devolve token
    [Fact]
    public async Task Login_com_senha_errada_devolve_401_sem_token()
    {
        using var app = new AdminApiFactory();
        using var client = app.CreateClient();

        var response = await client.PostAsync("/api/admin/login", Corpo("senha-que-nao-e-a-minha"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(corpo.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("error").GetString()));
        Assert.False(corpo.TryGetProperty("token", out _));
    }

    // ADM-02: a resposta não distingue senha errada de admin não configurado
    [Fact]
    public async Task Login_nao_revela_se_o_admin_esta_configurado()
    {
        using var configurado = new AdminApiFactory();
        using var semSenha = new AdminApiFactory(comSenha: false);

        var comErro = await configurado.CreateClient().PostAsync("/api/admin/login", Corpo("errada"));
        var semAdmin = await semSenha.CreateClient().PostAsync("/api/admin/login", Corpo("errada"));

        Assert.Equal(HttpStatusCode.Unauthorized, comErro.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, semAdmin.StatusCode);
        Assert.Equal(await comErro.Content.ReadAsStringAsync(), await semAdmin.Content.ReadAsStringAsync());
    }

    // ADM-04: sem hash configurado, nem a senha "certa" abre — fecha, não abre
    [Fact]
    public async Task Login_sem_hash_configurado_recusa_ate_a_senha_certa()
    {
        using var app = new AdminApiFactory(comSenha: false);
        using var client = app.CreateClient();

        var response = await client.PostAsync("/api/admin/login", Corpo(AdminApiFactory.SenhaCorreta));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ADM-04: chave de assinatura curta demais para HMAC-SHA256 conta como não configurado
    [Fact]
    public async Task Login_com_chave_de_assinatura_curta_recusa()
    {
        using var app = new AdminApiFactory(signingKey: "curta-demais");
        using var client = app.CreateClient();

        var response = await client.PostAsync("/api/admin/login", Corpo(AdminApiFactory.SenhaCorreta));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ADM-03: a sexta tentativa dentro da janela é recusada antes de conferir a senha
    [Fact]
    public async Task Login_recusa_a_sexta_tentativa_do_minuto_com_429()
    {
        using var app = new AdminApiFactory();
        using var client = app.CreateClient();

        var códigos = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
            códigos.Add((await client.PostAsync("/api/admin/login", Corpo("errada"))).StatusCode);

        Assert.All(códigos.Take(5), c => Assert.Equal(HttpStatusCode.Unauthorized, c));
        Assert.Equal(HttpStatusCode.TooManyRequests, códigos[5]);
    }

    // ADM-12: a leitura pública não passa a exigir autenticação por causa do login
    [Fact]
    public async Task Get_projects_continua_anonimo()
    {
        using var app = new AdminApiFactory();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
