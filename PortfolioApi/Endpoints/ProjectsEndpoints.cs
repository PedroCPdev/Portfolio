using Portfolio.Data;

namespace Portfolio.Endpoints;

public static class ProjectsEndpoints
{
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
    }
}
