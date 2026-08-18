using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;
using Portfolio.Data;

namespace Portfolio.Tests;

/// <summary>
/// Conecta no emulador Firestore. Falha explicitamente se o emulador não estiver no ar —
/// pular o teste em silêncio mascararia regressão real.
/// </summary>
public class FirestoreFixture
{
    public const string ProjectId = "demo-portfolio";
    public FirestoreDb Db { get; }

    public FirestoreFixture()
    {
        var host = Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "FIRESTORE_EMULATOR_HOST não está definida. Suba o emulador antes de rodar os testes:\n" +
                "  export JAVA_HOME=/usr/lib/jvm/java-25-openjdk\n" +
                "  npx -y firebase-tools@latest emulators:start --only firestore --project demo-portfolio\n" +
                "  export FIRESTORE_EMULATOR_HOST=127.0.0.1:8080");
        }

        Db = new FirestoreDbBuilder
        {
            ProjectId = ProjectId,
            EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
        }.Build();
    }

    /// <summary>Store apontando para uma coleção exclusiva deste teste (parallel-safe).</summary>
    public FirestoreProjectStore NewStore(out string collection)
    {
        collection = $"projects_test_{Guid.NewGuid():N}";
        return new FirestoreProjectStore(Db, Options.Create(new FirestoreOptions
        {
            ProjectId = ProjectId,
            ProjectsCollection = collection,
        }));
    }
}
