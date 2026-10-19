using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Models;
using Xunit;

namespace Portfolio.Tests;

/// <summary>
/// Escrita autenticada ponta a ponta: HTTP real contra a aplicação real, gravando no emulador
/// numa coleção exclusiva deste teste. Depende do <see cref="FirestoreFixture"/> só para falhar
/// alto quando o emulador não está no ar.
/// </summary>
[Trait("Category", "Firestore")]
public class AdminProjectsEndpointTests : IClassFixture<FirestoreFixture>
{
    public AdminProjectsEndpointTests(FirestoreFixture fixture) => _ = fixture;

    private static AdminApiFactory NovaApi() => new(extras: new Dictionary<string, string?>
    {
        ["Firestore:ProjectId"] = FirestoreFixture.ProjectId,
        ["Firestore:ProjectsCollection"] = $"projects_http_{Guid.NewGuid():N}",
    });

    private static async Task<HttpClient> Autenticado(AdminApiFactory api)
    {
        var client = api.CreateClient();
        var login = await client.PostAsync("/api/admin/login", Json(new { password = AdminApiFactory.SenhaCorreta }));
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static StringContent Json(object corpo) =>
        new(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

    private static object NovoProjeto(string titulo = "Projeto novo") => new
    {
        title = titulo,
        description = "descrição do projeto novo",
        tags = new[] { "C#", "Firestore" },
        githubUrl = "https://github.com/PedroCPdev/Portfolio",
        decisions = new[]
        {
            new { title = "primeira", why = "porque sim", cost = "custa isto" },
            new { title = "segunda", why = "porque também", cost = "" },
        },
    };

    // ADM-06 (sucesso)
    [Fact]
    public async Task Post_autenticado_cria_o_projeto_e_devolve_201_com_location()
    {
        using var api = NovaApi();
        var client = await Autenticado(api);

        var response = await client.PostAsync("/api/projects", Json(NovoProjeto()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var criado = await response.Content.ReadFromJsonAsync<Project>();
        Assert.NotNull(criado);
        Assert.False(string.IsNullOrWhiteSpace(criado!.Id));
        Assert.Equal($"/api/projects/{criado.Id}", response.Headers.Location?.ToString());
        Assert.Equal(DateTimeKind.Utc, criado.CreatedAt.Kind);

        var lido = await client.GetFromJsonAsync<Project>($"/api/projects/{criado.Id}");
        Assert.Equal("Projeto novo", lido!.Title);
        // ADM-11: a ordem das decisões é a que foi enviada
        Assert.Equal(["primeira", "segunda"], lido.Decisions.Select(d => d.Title));
    }

    // ADM-06: id e createdAt do corpo são descartados — quem manda é o servidor
    [Fact]
    public async Task Post_ignora_id_e_createdAt_enviados_pelo_cliente()
    {
        using var api = NovaApi();
        var client = await Autenticado(api);

        var response = await client.PostAsync("/api/projects", Json(new
        {
            id = "id-escolhido-pelo-cliente",
            createdAt = "1999-01-01T00:00:00Z",
            title = "Projeto",
            description = "descrição",
        }));

        var criado = await response.Content.ReadFromJsonAsync<Project>();
        Assert.NotEqual("id-escolhido-pelo-cliente", criado!.Id);
        Assert.True(criado.CreatedAt > new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    // ADM-07 (sucesso)
    [Fact]
    public async Task Put_autenticado_edita_e_preserva_o_createdAt_original()
    {
        using var api = NovaApi();
        var client = await Autenticado(api);
        var criado = await (await client.PostAsync("/api/projects", Json(NovoProjeto()))).Content.ReadFromJsonAsync<Project>();
        // O eco do POST vem do objeto em memória; o Firestore grava truncando em microssegundo
        // (ver L-008). A data que interessa preservar é a GRAVADA.
        var gravado = await client.GetFromJsonAsync<Project>($"/api/projects/{criado!.Id}");

        var response = await client.PutAsync($"/api/projects/{criado.Id}", Json(new
        {
            createdAt = "1999-01-01T00:00:00Z",
            title = "Título editado",
            description = "descrição editada",
            tags = new[] { "Next.js" },
            decisions = new[] { new { title = "só uma", why = "sobrou uma", cost = "" } },
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var atualizado = await response.Content.ReadFromJsonAsync<Project>();
        Assert.Equal("Título editado", atualizado!.Title);
        Assert.Equal(gravado!.CreatedAt, atualizado.CreatedAt);
        Assert.Equal(gravado.CreatedAt, (await client.GetFromJsonAsync<Project>($"/api/projects/{criado.Id}"))!.CreatedAt);
        Assert.Single(atualizado.Decisions);
    }

    // ADM-08 (sem sucesso)
    [Fact]
    public async Task Put_e_delete_em_id_inexistente_devolvem_404_sem_criar()
    {
        using var api = NovaApi();
        var client = await Autenticado(api);

        // O DataSeeder semeia dois projetos de exemplo em coleção vazia a cada startup, então
        // "não criou nada" se mede pela contagem não ter mudado, não por a lista estar vazia.
        var antes = (await client.GetFromJsonAsync<List<Project>>("/api/projects"))!.Count;

        var put = await client.PutAsync("/api/projects/id-que-nao-existe", Json(NovoProjeto()));
        var delete = await client.DeleteAsync("/api/projects/id-que-nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(antes, (await client.GetFromJsonAsync<List<Project>>("/api/projects"))!.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/projects/id-que-nao-existe")).StatusCode);
    }

    // C-13: o %2F chega ao handler já decodificado como "a/b"
    [Fact]
    public async Task Get_com_barra_codificada_no_id_devolve_404()
    {
        using var api = NovaApi();
        var client = api.CreateClient();

        var resposta = await client.GetAsync("/api/projects/a%2Fb");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // ADM-09 (sucesso)
    [Fact]
    public async Task Delete_autenticado_remove_e_o_get_seguinte_da_404()
    {
        using var api = NovaApi();
        var client = await Autenticado(api);
        var criado = await (await client.PostAsync("/api/projects", Json(NovoProjeto()))).Content.ReadFromJsonAsync<Project>();

        var delete = await client.DeleteAsync($"/api/projects/{criado!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/projects/{criado.Id}")).StatusCode);
    }

    // ADM-05 (falha): sem token, com token expirado e com assinatura adulterada
    [Fact]
    public async Task Escrita_sem_token_valido_devolve_401_e_nao_grava()
    {
        using var api = NovaApi();
        var autenticado = await Autenticado(api);
        var criado = await (await autenticado.PostAsync("/api/projects", Json(NovoProjeto("original")))).Content.ReadFromJsonAsync<Project>();

        var anonimo = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync("/api/projects", Json(NovoProjeto()))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PutAsync($"/api/projects/{criado!.Id}", Json(NovoProjeto("invadido")))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.DeleteAsync($"/api/projects/{criado.Id}")).StatusCode);

        var expirado = api.CreateClient();
        expirado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenExpirado(api.SigningKey));
        Assert.Equal(HttpStatusCode.Unauthorized, (await expirado.DeleteAsync($"/api/projects/{criado.Id}")).StatusCode);

        var adulterado = api.CreateClient();
        var bom = autenticado.DefaultRequestHeaders.Authorization!.Parameter!;
        adulterado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bom[..^1] + (bom[^1] == 'A' ? 'B' : 'A'));
        Assert.Equal(HttpStatusCode.Unauthorized, (await adulterado.DeleteAsync($"/api/projects/{criado.Id}")).StatusCode);

        // nada disso pode ter mudado o documento
        var lido = await autenticado.GetFromJsonAsync<Project>($"/api/projects/{criado.Id}");
        Assert.Equal("original", lido!.Title);
    }

    // ADM-10 (falha): corpo inválido não grava
    [Fact]
    public async Task Post_com_corpo_invalido_devolve_400_no_shape_da_casa_e_nao_grava()
    {
        using var api = NovaApi();
        var client = await Autenticado(api);

        var antes = (await client.GetFromJsonAsync<List<Project>>("/api/projects"))!.Count;

        var response = await client.PostAsync("/api/projects", Json(new
        {
            title = "   ",
            description = "descrição",
            decisions = new[] { new { title = "sem porquê", why = "", cost = "" } },
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(corpo.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("error").GetString()));
        Assert.Equal(antes, (await client.GetFromJsonAsync<List<Project>>("/api/projects"))!.Count);
    }

    // ADM-10 / D-11: os endpoints de escrita declaram o teto de 64 KB para o corpo.
    //
    // Por que metadata e não uma requisição de 100 KB: o TestServer em memória não aplica
    // IHttpMaxRequestBodySizeFeature, então a requisição chega inteira e o que responde é a
    // validação (400), mascarando o mecanismo. Contra Kestrel real o mesmo corpo devolve 413 —
    // verificado e registrado em evidencias/20260824-limite-de-corpo-kestrel.md.
    [Fact]
    public void Endpoints_de_escrita_declaram_o_limite_de_corpo_de_64_kb()
    {
        using var api = NovaApi();
        var fonte = api.Services.GetRequiredService<EndpointDataSource>();

        var escrita = fonte.Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText!.StartsWith("/api/projects")
                        && e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                            .Any(m => m is "POST" or "PUT") == true)
            .ToList();

        Assert.Equal(2, escrita.Count);
        Assert.All(escrita, e =>
            Assert.Equal(64 * 1024, e.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize));
    }

    // ADM-12: a leitura pública não regride
    [Fact]
    public async Task Get_publico_continua_anonimo_depois_da_escrita_existir()
    {
        using var api = NovaApi();
        var autenticado = await Autenticado(api);
        await autenticado.PostAsync("/api/projects", Json(NovoProjeto("visível")));

        var anonimo = api.CreateClient();
        var lista = await anonimo.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var projetos = await lista.Content.ReadFromJsonAsync<List<Project>>() ?? [];
        Assert.Contains(projetos, p => p.Title == "visível");
    }

    private static string TokenExpirado(string signingKey) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "portfolio-api",
            Audience = "portfolio-admin",
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")]),
            Expires = DateTime.UtcNow.AddMinutes(-1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        });
}
