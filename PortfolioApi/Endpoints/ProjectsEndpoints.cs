using Microsoft.AspNetCore.Mvc;
using Portfolio.Data;
using Portfolio.Models;
using Portfolio.Validation;

namespace Portfolio.Endpoints;

public static class ProjectsEndpoints
{
    /// <summary>
    /// Pior caso legítimo é ~35 KB (descrição de 2000 chars + 10 decisões em UTF-8). O dobro de
    /// folga, e ainda 500× menor que os 30 MB que o Kestrel aceita por padrão. Aplicado como
    /// metadata do endpoint, não como limite global: um limite no Kestrel valeria para qualquer
    /// endpoint futuro sem que quem o escrevesse percebesse.
    /// </summary>
    private const int LimiteDoCorpoEmBytes = 64 * 1024;

    public static void MapProjectsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects");

        group.MapGet("/", async (FirestoreProjectStore store, CancellationToken cancellationToken) =>
            Results.Ok(await store.GetAllAsync(cancellationToken)));

        // Id do Firestore é string (20 caracteres), não int — a restrição :int saiu da rota.
        group.MapGet("/{id}", async (string id, FirestoreProjectStore store, CancellationToken cancellationToken) =>
        {
            var project = await store.GetByIdAsync(id, cancellationToken);
            return project is null ? Results.NotFound() : Results.Ok(project);
        });

        // ─── Escrita: só com JWT de admin ───
        // O corpo desserializa direto no model (sem DTO, conforme CONVENTIONS.md), então o
        // cliente PODE mandar `id` e `createdAt`. Os dois são descartados aqui: o id vem do
        // Firestore ou da rota, e a data de criação é do servidor. Isso não é expresso pelo
        // compilador — quem segura é o teste.
        group.MapPost("/", async (Project project, FirestoreProjectStore store, CancellationToken cancellationToken) =>
        {
            project.CreatedAt = DateTime.UtcNow;
            project.Id = await store.AddAsync(project, cancellationToken);

            return Results.Created($"/api/projects/{project.Id}", project);
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<Project>>()
        .WithMetadata(new RequestSizeLimitAttribute(LimiteDoCorpoEmBytes));

        group.MapPut("/{id}", async (string id, Project project, FirestoreProjectStore store, CancellationToken cancellationToken) =>
        {
            // UpdateAsync repõe no objeto o createdAt já gravado, então a resposta sai com a
            // data verdadeira e não com a que o cliente mandou.
            if (!await store.UpdateAsync(id, project, cancellationToken)) return Results.NotFound();

            project.Id = id;
            return Results.Ok(project);
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<Project>>()
        .WithMetadata(new RequestSizeLimitAttribute(LimiteDoCorpoEmBytes));

        group.MapDelete("/{id}", async (string id, FirestoreProjectStore store, CancellationToken cancellationToken) =>
            await store.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound())
        .RequireAuthorization();
    }
}
