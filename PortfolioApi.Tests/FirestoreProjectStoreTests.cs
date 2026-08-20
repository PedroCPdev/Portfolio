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

    // PD-01: a lista de decisões faz round-trip, incluindo a ordem e os três campos de cada uma
    [Fact]
    public async Task AddAsync_faz_round_trip_da_lista_de_decisoes()
    {
        var store = _fixture.NewStore(out _);
        var project = NewProject("com decisões", DateTime.UtcNow);
        project.Decisions =
        [
            new Decision
            {
                Title = "Firestore over PostgreSQL",
                Why = "O Postgres pausava por inatividade e derrubava a API junto.",
                Cost = "Sem SQL, sem joins, sem migrations versionadas.",
            },
            new Decision
            {
                Title = "Keep-alive de 16h",
                Why = "744 das 750 instance-hours do mês.",
                Cost = "A madrugada paga o cold start.",
            },
        ];

        var id = await store.AddAsync(project);

        var found = await store.GetByIdAsync(id);
        Assert.NotNull(found);
        Assert.Equal(2, found!.Decisions.Length);
        Assert.Equal("Firestore over PostgreSQL", found.Decisions[0].Title);
        Assert.Equal("O Postgres pausava por inatividade e derrubava a API junto.", found.Decisions[0].Why);
        Assert.Equal("Sem SQL, sem joins, sem migrations versionadas.", found.Decisions[0].Cost);
        // A ordem é informação: é a sequência em que o autor quer que sejam lidas.
        Assert.Equal("Keep-alive de 16h", found.Decisions[1].Title);
    }

    // PD-02: projeto sem decisões devolve lista vazia, não null — quem consome não checa null
    [Fact]
    public async Task AddAsync_sem_decisoes_devolve_lista_vazia()
    {
        var store = _fixture.NewStore(out _);

        var id = await store.AddAsync(NewProject("sem decisões", DateTime.UtcNow));

        var found = await store.GetByIdAsync(id);
        Assert.NotNull(found);
        Assert.Empty(found!.Decisions);
    }

    // PD-02: este é o risco real da mudança. Todo documento que já está em produção foi gravado
    // sem a chave `decisions`. Se a leitura estourasse com ela ausente, GET /api/projects
    // quebraria inteiro no primeiro deploy — e um teste que grava pelo próprio model nunca
    // pegaria isso, porque o model sempre grava a chave. Por isso o documento aqui é cru.
    [Fact]
    public async Task GetAllAsync_le_documento_sem_a_chave_decisions_sem_lancar()
    {
        var store = _fixture.NewStore(out var collection);
        await _fixture.Db.Collection(collection).AddAsync(new Dictionary<string, object>
        {
            ["title"] = "documento legado",
            ["description"] = "gravado antes de decisions existir",
            ["tags"] = new[] { "C#" },
            ["createdAt"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        var all = await store.GetAllAsync();

        var legacy = Assert.Single(all);
        Assert.Equal("documento legado", legacy.Title);
        Assert.Empty(legacy.Decisions);
    }

    // PD-01: decisão com campos ausentes não vira null no meio da lista
    [Fact]
    public async Task AddAsync_preserva_decisao_com_custo_vazio()
    {
        var store = _fixture.NewStore(out _);
        var project = NewProject("decisão sem custo", DateTime.UtcNow);
        project.Decisions = [new Decision { Title = "Só o porquê", Why = "Motivo." }];

        var id = await store.AddAsync(project);

        var found = await store.GetByIdAsync(id);
        var only = Assert.Single(found!.Decisions);
        Assert.Equal("Só o porquê", only.Title);
        Assert.True(string.IsNullOrEmpty(only.Cost));
    }

    // PD-01: o seed é a única fonte de exemplo para quem sobe o projeto do zero
    [Fact]
    public async Task SeedAsync_semeia_projetos_com_decisoes()
    {
        var store = _fixture.NewStore(out _);

        await DataSeeder.SeedAsync(store, NullLogger.Instance);

        var all = await store.GetAllAsync();
        Assert.All(all, p => Assert.NotEmpty(p.Decisions));
        Assert.All(all, p => Assert.All(p.Decisions, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Title));
            Assert.False(string.IsNullOrWhiteSpace(d.Why));
            Assert.False(string.IsNullOrWhiteSpace(d.Cost));
        }));
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
