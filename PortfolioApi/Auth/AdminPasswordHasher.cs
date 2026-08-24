using System.Security.Cryptography;

namespace Portfolio.Auth;

/// <summary>
/// Deriva e confere a senha de admin. O que fica guardado na variável de ambiente é
/// <c>{iterações}.{salt-base64}.{hash-base64}</c> — nunca a senha.
///
/// As 210 000 iterações são o custo deliberado: ~100 ms por tentativa é irrelevante para uma
/// pessoa que entra algumas vezes por semana e caro para quem tenta força bruta. Ver design D-2.
/// </summary>
public static class AdminPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    /// <summary>Gera o valor a colar em <c>Admin__PasswordHash</c>.</summary>
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(password, salt, Iterations);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Falso para senha vazia e para qualquer hash ausente ou corrompido — nunca lança.
    /// Configuração quebrada tem que fechar a porta, não derrubar a API nem abri-la (ADM-04).
    /// </summary>
    public static bool Verify(string? password, string? encoded)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(encoded)) return false;

        var parts = encoded.Split('.');
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out var iterations) || iterations <= 0) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || expected.Length == 0) return false;

        var actual = Derive(password, salt, iterations, expected.Length);
        // Comparação de tempo fixo: `==` sairia no primeiro byte diferente e vazaria,
        // pelo tempo de resposta, quanto do hash já está correto.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length = HashBytes) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, length);
}
