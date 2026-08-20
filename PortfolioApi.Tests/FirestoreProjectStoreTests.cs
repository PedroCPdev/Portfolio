using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Models;
using Portfolio.Services;
using Xunit;

namespace Portfolio.Tests;

public class FirestoreProjectStoreTests : IClassFixture<FirestoreFixture>
{
    private readonly FirestoreFixture _fixture;

    public FirestoreProjectStoreTests(FirestoreFixture fixture) => _fixture = fixture;

    private static Project NewProject(string title, DateTime createdAt) => new()
    {
        Title = title,
        Description = $"descrição de {title}",
        Tags = ["C#", "Firestore"],
        GithubUrl = "https://github.com/PedroCPdev/Portfolio",
        CreatedAt = createdAt,
    };

    // FS-02: lista ordenada por createdAt decrescente
    [Fact]
    public async Task GetAllAsync_retorna_projetos_do_mais_recente_para_o_mais_antigo()
    {
        var store = _fixture.NewStore(out _);
        await store.AddAsync(NewProject("antigo", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await store.AddAsync(NewProject("recente", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await store.AddAsync(NewProject("meio", new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await store.GetAllAsync();

        Assert.Equal(new[] { "recente", "meio", "antigo" }, result.Select(p => p.Title).ToArray());
    }

    // FS-02: coleção vazia devolve lista vazia, não erro
    [Fact]
    public async Task GetAllAsync_com_colecao_vazia_retorna_lista_vazia()
    {
        var store = _fixture.NewStore(out _);
        Assert.Empty(await store.GetAllAsync());
    }

    // FS-03: id do documento é devolvido preenchido, e os campos fazem round-trip
    [Fact]
    public async Task GetByIdAsync_devolve_projeto_com_id_do_documento_e_campos_intactos()
    {
        var store = _fixture.NewStore(out _);
        var created = NewProject("Portfolio API", new DateTime(2026, 5, 4, 12, 30, 0, DateTimeKind.Utc));
        created.LiveUrl = "https://pedrocpdev.vercel.app";
        var id = await store.AddAsync(created);

        var found = await store.GetByIdAsync(id);

        Assert.NotNull(found);
        Assert.Equal(id, found!.Id);
        Assert.Equal("Portfolio API", found.Title);
        Assert.Equal(new[] { "C#", "Firestore" }, found.Tags);
        Assert.Equal("https://pedrocpdev.vercel.app", found.LiveUrl);
        Assert.Null(found.ImageUrl);
        Assert.Equal(new DateTime(2026, 5, 4, 12, 30, 0, DateTimeKind.Utc), found.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, found.CreatedAt.Kind);
    }

    // FS-04: documento inexistente vira null, sem exceção
    [Fact]
    public async Task GetByIdAsync_com_id_inexistente_retorna_null_sem_lancar()
    {
        var store = _fixture.NewStore(out _);
        Assert.Null(await store.GetByIdAsync("id-que-nao-existe-000"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_com_id_vazio_retorna_null(string id)
    {
        var store = _fixture.NewStore(out _);
        Assert.Null(await store.GetByIdAsync(id));
    }

    // FS-05: DateTime não-UTC é normalizado em vez de estourar ArgumentException
    [Fact]
    public async Task AddAsync_aceita_createdAt_nao_utc_normalizando_para_utc()
    {
        var store = _fixture.NewStore(out _);
        var unspecified = new DateTime(2024, 3, 2, 10, 0, 0, DateTimeKind.Unspecified);

        var id = await store.AddAsync(NewProject("kind unspecified", unspecified));

        var found = await store.GetByIdAsync(id);
        Assert.NotNull(found);
        Assert.Equal(DateTimeKind.Utc, found!.CreatedAt.Kind);
        Assert.Equal(new DateTime(2024, 3, 2, 10, 0, 0, DateTimeKind.Utc), found.CreatedAt);
    }

    [Fact]
    public async Task AddAsync_converte_createdAt_local_para_o_instante_utc_equivalente()
    {
        var store = _fixture.NewStore(out _);
        var local = new DateTime(2024, 3, 2, 10, 0, 0, DateTimeKind.Local);

        var id = await store.AddAsync(NewProject("kind local", local));

        var found = await store.GetByIdAsync(id);
        Assert.NotNull(found);
        Assert.Equal(local.ToUniversalTime(), found!.CreatedAt);
    }

    // FS-06: seed idempotente
    [Fact]
    public async Task SeedAsync_popula_colecao_vazia_com_dois_projetos()
    {
        var store = _fixture.NewStore(out _);

        await DataSeeder.SeedAsync(store, NullLogger.Instance);

        Assert.Equal(2, await store.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_rodado_duas_vezes_nao_duplica_projetos()
    {
        var store = _fixture.NewStore(out _);

        await DataSeeder.SeedAsync(store, NullLogger.Instance);
        await DataSeeder.SeedAsync(store, NullLogger.Instance);

        Assert.Equal(2, await store.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_nao_semeia_quando_ja_existe_projeto()
    {
        var store = _fixture.NewStore(out _);
        await store.AddAsync(NewProject("preexistente", DateTime.UtcNow));

        await DataSeeder.SeedAsync(store, NullLogger.Instance);

        Assert.Equal(1, await store.CountAsync());
    }

    // TR-01: o custo do trade-off sobrevive à ida e volta no Firestore
    [Fact]
    public async Task AddAsync_faz_round_trip_do_tradeoff()
    {
        var store = _fixture.NewStore(out _);
        var project = NewProject("com custo", DateTime.UtcNow);
        project.Tradeoff = "Sem SQL, sem joins, sem migrations versionadas.";

        var id = await store.AddAsync(project);

        var found = await store.GetByIdAsync(id);
        Assert.NotNull(found);
        Assert.Equal("Sem SQL, sem joins, sem migrations versionadas.", found!.Tradeoff);
    }

    // TR-01: campo ausente é legítimo — o ledger degrada sem ele
    [Fact]
    public async Task AddAsync_sem_tradeoff_devolve_null()
    {
        var store = _fixture.NewStore(out _);

        var id = await store.AddAsync(NewProject("sem custo", DateTime.UtcNow));

        var found = await store.GetByIdAsync(id);
        Assert.NotNull(found);
        Assert.Null(found!.Tradeoff);
    }

    // TR-02: o Firestore é schemaless, então os documentos que já estão em produção não têm a
    // chave `tradeoff`. Este teste grava um documento cru sem ela — como se tivesse sido escrito
    // antes do campo existir — e prova que a leitura não estoura. É o único risco real da mudança.
    [Fact]
    public async Task GetAllAsync_le_documento_gravado_antes_do_campo_existir_sem_lancar()
    {
        var store = _fixture.NewStore(out var collection);
        await _fixture.Db.Collection(collection).AddAsync(new Dictionary<string, object>
        {
            ["title"] = "documento legado",
            ["description"] = "gravado antes de tradeoff existir",
            ["tags"] = new[] { "C#" },
            ["createdAt"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        var all = await store.GetAllAsync();

        var legacy = Assert.Single(all);
        Assert.Equal("documento legado", legacy.Title);
        Assert.Null(legacy.Tradeoff);
    }

    // TR-04: o seed é a única fonte de exemplo do ledger para quem sobe o projeto do zero
    [Fact]
    public async Task SeedAsync_semeia_projetos_com_tradeoff_preenchido()
    {
        var store = _fixture.NewStore(out _);

        await DataSeeder.SeedAsync(store, NullLogger.Instance);

        var all = await store.GetAllAsync();
        Assert.All(all, p => Assert.False(string.IsNullOrWhiteSpace(p.Tradeoff)));
    }

    [Fact]
    public async Task CountAsync_reflete_quantidade_de_documentos()
    {
        var store = _fixture.NewStore(out _);
        Assert.Equal(0, await store.CountAsync());

        await store.AddAsync(NewProject("um", DateTime.UtcNow));
        Assert.Equal(1, await store.CountAsync());
    }
}
