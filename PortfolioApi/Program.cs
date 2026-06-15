// ─── FASE 1: Services ───────────────────────

using Microsoft.EntityFrameworkCore;
using Portfolio.Data;
using Portfolio.Endpoints;
using Portfolio.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                builder.Configuration["AllowedOrigins"] ?? "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

// ─── FASE 2: Migrations + Seed ───────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();   // aplica migrations automaticamente
    await DataSeeder.SeedAsync(db);     // insere dados iniciais se estiver vazio
}

// ─── FASE 3: Pipeline + Endpoints ────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("Frontend");

app.MapContactEndpoints();
app.MapProjectsEndpoints();

app.UseHttpsRedirection();

app.Run();