using Portfolio.Data;
using Portfolio.Models;

namespace Portfolio.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(
        FirestoreProjectStore store,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await store.CountAsync(cancellationToken) > 0) return;

        Project[] seedProjects =
        [
            new()
            {
                Title = "Portfolio API",
                Description = "RESTful API built with ASP.NET Core 9, Google Cloud Firestore and Scalar. exposes projects endpoint via Next.js.",
                Tradeoff = "It runs on a free tier that sleeps after 15 minutes of no traffic. The first visitor after a quiet night waits about a minute for the container to wake, and the frontend retries for 54 seconds to cover it.",
                Tags = ["C#", ".NET 9", "Firestore", "NoSQL", "Scalar"],
                GithubUrl = "https://github.com/PedroCPdev/Portfolio",
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Title = "Portfolio Frontend",
                Description = "Personal Portfolio built using Next.js 16 and React 19. Minimalistic dark design, C# API integrated.",
                Tradeoff = "There is no admin area, so adding a project means writing the document by hand in the Firebase console. With ISR at 60 seconds, the change also takes up to a minute to show up.",
                Tags = ["TypeScript", "Next.js", "React", "Tailwind CSS"],
                GithubUrl = "https://github.com/PedroCPdev/Portfolio",
                LiveUrl = "https://pedrocpdev.vercel.app",
                CreatedAt = DateTime.UtcNow,
            },
        ];

        foreach (var project in seedProjects)
            await store.AddAsync(project, cancellationToken);

        logger.LogInformation("[Seeder] {Count} projetos semeados no Firestore.", seedProjects.Length);
    }
}
