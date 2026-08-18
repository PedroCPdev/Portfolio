// ─── FASE 1: Services ───────────────────────

using System.Threading.RateLimiting;
using Google.Apis.Auth.OAuth2;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Portfolio.Data;
using Portfolio.Endpoints;
using Portfolio.Services;
using Scalar.AspNetCore;

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

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("contact", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
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

app.UseCors("Frontend");
app.UseRateLimiter();

app.MapHealthEndpoints();
app.MapContactEndpoints();
app.MapProjectsEndpoints();

app.Run();
