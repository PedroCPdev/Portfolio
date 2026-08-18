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
                Tags = ["C#", ".NET 9", "Firestore", "NoSQL", "Scalar"],
                GithubUrl = "https://github.com/PedroCPdev/Portfolio",
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Title = "Portfolio Frontend",
                Description = "Personal Portfolio built using Next.js 16 and React 19. Minimalistic dark design, C# API integrated.",
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
