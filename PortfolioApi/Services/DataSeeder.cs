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
                Decisions =
                [
                    new Decision
                    {
                        Title = "Firestore over a relational database",
                        Why = "The managed Postgres it replaced paused on inactivity, and a paused database took the whole API down with it — including endpoints that never touched a database. Firestore's free tier does not pause.",
                        Cost = "No SQL, no joins, no versioned migrations. Document ids stopped being integers, so the public contract changed and the frontend changed with it.",
                    },
                    new Decision
                    {
                        Title = "A seed failure no longer stops startup",
                        Why = "A database outage used to take down endpoints that did not need the database at all. Logging and carrying on turns a total outage into a partial one.",
                        Cost = "The API can now report itself healthy while serving an empty project list.",
                    },
                ],
                Tags = ["C#", ".NET 9", "Firestore", "NoSQL", "Scalar"],
                GithubUrl = "https://github.com/PedroCPdev/Portfolio",
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Title = "Portfolio Frontend",
                Description = "Personal Portfolio built using Next.js 16 and React 19. Minimalistic dark design, C# API integrated.",
                Decisions =
                [
                    new Decision
                    {
                        Title = "Server rendering with a 60 second cache",
                        Why = "The project list changes a few times a year. Rendering it on the server keeps the page fast and indexable without shipping a data-fetching layer to the browser.",
                        Cost = "A newly published project can take up to a minute to appear, and the page cannot show anything personalised per visitor.",
                    },
                    new Decision
                    {
                        Title = "No admin area",
                        Why = "An authenticated CRUD would be the largest part of this codebase, to be used a handful of times a year by one person.",
                        Cost = "Publishing a project means writing the document by hand in the Firebase console.",
                    },
                ],
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
