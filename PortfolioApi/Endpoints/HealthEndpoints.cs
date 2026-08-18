namespace Portfolio.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        // Alvo do keep-alive. Deliberadamente NÃO toca no Firestore: o ping roda a cada
        // poucos minutos e consultar o banco a cada vez gastaria cota de leitura sem
        // nenhum ganho — o objetivo é apenas manter a instância do Render acordada.
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithTags("Health")
            .ExcludeFromDescription();
    }
}
