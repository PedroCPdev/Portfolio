// ─── FASE 1: Services ───────────────────────

using Google.Apis.Auth.OAuth2;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Portfolio.Data;
using Portfolio.Endpoints;
using Portfolio.Services;
using Portfolio.Auth;
using Portfolio.RateLimiting;
using Scalar.AspNetCore;

// Gerador do valor de Admin__PasswordHash. Roda antes de qualquer coisa de servidor web e sai:
// `dotnet run -- hash-password <senha>`. Existe para que a senha nunca precise passar por um
// gerador de hash online para virar variável de ambiente (design D-3).
if (args is ["hash-password", var senhaEmTextoPuro, ..])
{
    Console.WriteLine(AdminPasswordHasher.Hash(senhaEmTextoPuro));
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                builder.Configuration["AllowedOrigins"] ?? "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<FirestoreOptions>(builder.Configuration.GetSection("Firestore"));

builder.Services.AddSingleton<FirestoreDb>(sp =>
{
    var options = sp.GetRequiredService<IOptions<FirestoreOptions>>().Value;
    var dbBuilder = new FirestoreDbBuilder
    {
        ProjectId = options.ProjectId,
        // Sem isto o padrão é None e a variável FIRESTORE_EMULATOR_HOST seria ignorada,
        // impedindo o desenvolvimento local contra o emulador. Em produção a variável
        // não existe e o cliente vai para o Firestore real.
        EmulatorDetection = EmulatorDetection.EmulatorOrProduction,
    };

    // Credencial explícita via JSON da service account (Render, sem arquivo em disco).
    // Sem ela, cai nas Application Default Credentials — que é também o caminho usado
    // pelo emulador local (FIRESTORE_EMULATOR_HOST).
    if (!string.IsNullOrWhiteSpace(options.CredentialsJson))
    {
        var serviceAccount = CredentialFactory.FromJson<ServiceAccountCredential>(options.CredentialsJson);
        dbBuilder.GoogleCredential = GoogleCredential.FromServiceAccountCredential(serviceAccount);
    }

    return dbBuilder.Build();
});

builder.Services.AddSingleton<FirestoreProjectStore>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<EmailService>();

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.AddSingleton<AdminTokenService>();

// A validação do token precisa da chave no momento em que o esquema é registrado, antes de
// existir um provedor de serviços para injetar IOptions — daí a leitura direta da configuração.
var adminOptions = builder.Configuration.GetSection("Admin").Get<AdminOptions>() ?? new AdminOptions();
if (!adminOptions.IsConfigured)
{
    Console.WriteLine(
        "[Admin] Área administrativa DESLIGADA: Admin__PasswordHash e/ou Admin__JwtSigningKey " +
        "ausentes (a chave precisa de 32 bytes ou mais). Login e escrita respondem 401.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = AdminTokenService.ValidationParameters(adminOptions));
builder.Services.AddAuthorization();

// Atrás do proxy do Render o IP do visitante só existe no X-Forwarded-For, e sem isto o
// RemoteIpAddress é o do proxy — todo mundo na mesma partição, que era o C-6.
// Os defaults não servem: ForwardedHeaders é None e KnownProxies traz só o loopback, então
// UseForwardedHeaders sem configuração é inerte fora de localhost (verificado).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;

    // ForwardLimit = 1 (o padrão, explícito por ser a garantia): o middleware lê a entrada mais
    // à DIREITA do cabeçalho, a que o proxy anexou. Subir para 2 o faz andar um salto à esquerda
    // e passar a acreditar no que o cliente escreveu — aí sim a partição vira falsificável.
    options.ForwardLimit = 1;

    // O container do Render só é alcançável pelo proxy deles, e o IP desse proxy não é estável o
    // bastante para virar lista mantida à mão.
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    // 429 é a resposta certa para excesso de requisições; o padrão do limitador é 503, que
    // anuncia "serviço fora do ar". Vale para todas as políticas — inclusive a do login, cujo
    // OnRejected (D-10 do admin-panel) passou a ser redundante, não errado.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Encadeamento do C-6: janela por visitante e teto do endpoint, nesta ordem. Registrado como
    // GlobalLimiter em vez de política do endpoint porque é a única forma de garantir que uma
    // recusa por visitante não consuma o teto — ver ContactRateLimiter.
    options.GlobalLimiter = ContactRateLimiter.Criar();

    options.AddPolicy(LoginRateLimitPolicy.Name, new LoginRateLimitPolicy());
});

var app = builder.Build();

// ─── FASE 2: Seed ────────────────────────────
// Firestore é schemaless: não há migrations a aplicar.
// Uma falha aqui NÃO derruba a aplicação — antes, o throw deixava a API inteira fora do ar
// (inclusive /api/contact, que nem usa banco) sempre que o banco estava indisponível.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var store = scope.ServiceProvider.GetRequiredService<FirestoreProjectStore>();
        await DataSeeder.SeedAsync(store, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex,
            "Seed do Firestore falhou; a API segue no ar e /api/projects pode responder vazio. " +
            "Verifique Firestore__ProjectId e Firestore__CredentialsJson.");
    }
}

// ─── FASE 3: Pipeline + Endpoints ────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler();
}

// Primeiro de todos: o rate limit particiona por RemoteIpAddress, e qualquer log de IP antes
// disto registraria o proxy no lugar do visitante.
app.UseForwardedHeaders();

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapContactEndpoints();
app.MapProjectsEndpoints();
app.MapAdminEndpoints();

app.Run();

// Torna a classe gerada pelos top-level statements visível para WebApplicationFactory nos testes.
// Sem isto ela é internal e o host de teste não consegue subir a aplicação real.
public partial class Program;
