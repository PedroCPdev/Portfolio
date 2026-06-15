using Portfolio.Data;
using Portfolio.Models;

namespace Portfolio.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Projects.Any()) return;

        db.Projects.AddRange(
            new Project
            {
                Title = "Portfolio API",
                Description = "RESTful API built with ASP.NET Core 9, Entity Framework Core and PostgreSQL. exposes projects endpoint via Next.js.",
                Tags = ["C#", ".NET 9", "EF Core", "PostgreSQL", "Scalar"],
                GithubUrl = "https://github.com/PedroCPdev/Portfolio",
                CreatedAt = DateTime.UtcNow,
            },
            new Project
            {
                Title = "Portfolio Frontend",
                Description = "Personal Portfolio built using Next.js 16 and React 19. Minimalistic dark design, C# API integrated.",
                Tags = ["TypeScript", "Next.js", "React", "Tailwind CSS"],
                GithubUrl = "https://github.com/PedroCPdev/Portfolio",
                LiveUrl = "https://pedrocpdev.vercel.app",
                CreatedAt = DateTime.UtcNow,
            }
        );

        await db.SaveChangesAsync();
        Console.WriteLine("[Seeder] Projetos successfully seeded.");
    }
}