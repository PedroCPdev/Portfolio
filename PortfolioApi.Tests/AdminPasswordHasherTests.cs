using Portfolio.Auth;
using Xunit;

namespace Portfolio.Tests;

/// <summary>
/// A senha de admin nunca é guardada em texto puro (design D-2): o que vai na variável de
/// ambiente é `{iterações}.{salt-base64}.{hash-base64}`.
/// </summary>
public class AdminPasswordHasherTests
{
    // ADM-02: a senha correta abre
    [Fact]
    public void Verify_aceita_a_senha_que_gerou_o_hash()
    {
        var encoded = AdminPasswordHasher.Hash("senha-do-pedro-2026");

        Assert.True(AdminPasswordHasher.Verify("senha-do-pedro-2026", encoded));
    }

    // ADM-02: senha errada do mesmo tamanho não abre — o tamanho não é o que decide
    [Fact]
    public void Verify_recusa_senha_errada_do_mesmo_tamanho()
    {
        var encoded = AdminPasswordHasher.Hash("senha-do-pedro-2026");

        Assert.False(AdminPasswordHasher.Verify("senha-do-pedro-2027", encoded));
    }

    // ADM-04: hash ausente ou corrompido não abre e não derruba a aplicação
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-pontos")]
    [InlineData("210000.soh-uma-parte")]
    [InlineData("nao-e-numero.c2FsdA==.aGFzaA==")]
    [InlineData("210000.nao-e-base64!.aGFzaA==")]
    [InlineData("0.c2FsdA==.aGFzaA==")]
    public void Verify_recusa_hash_ausente_ou_malformado_sem_lancar(string? encoded)
    {
        Assert.False(AdminPasswordHasher.Verify("qualquer-senha", encoded));
    }

    // ADM-02: senha vazia nunca abre, mesmo contra um hash válido de senha vazia
    [Fact]
    public void Verify_recusa_senha_vazia()
    {
        var encoded = AdminPasswordHasher.Hash("senha-real");

        Assert.False(AdminPasswordHasher.Verify("", encoded));
        Assert.False(AdminPasswordHasher.Verify("   ", encoded));
    }

    // D-2: dois hashes da mesma senha diferem — há salt por hash, não um salt fixo no código
    [Fact]
    public void Hash_usa_salt_novo_a_cada_chamada()
    {
        var a = AdminPasswordHasher.Hash("mesma-senha");
        var b = AdminPasswordHasher.Hash("mesma-senha");

        Assert.NotEqual(a, b);
        Assert.True(AdminPasswordHasher.Verify("mesma-senha", a));
        Assert.True(AdminPasswordHasher.Verify("mesma-senha", b));
    }
}
