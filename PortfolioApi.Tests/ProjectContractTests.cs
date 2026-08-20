using System.Text.Json;
using Portfolio.Models;
using Xunit;

namespace Portfolio.Tests;

/// <summary>
/// Contrato público de <see cref="Project"/>. Não toca no Firestore: o que está sob teste aqui é
/// o JSON que o frontend consome, e ele quebra sem aviso do compilador porque não há DTO (C-4).
/// </summary>
public class ProjectContractTests
{
    // A API serializa com as opções padrão do ASP.NET Core (camelCase).
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    // TR-03: o frontend lê `tradeoff`; qualquer outro nome deixa a linha de custo vazia em silêncio.
    [Fact]
    public void Project_serializa_tradeoff_em_camelCase()
    {
        var json = JsonSerializer.Serialize(
            new Project { Title = "t", Tradeoff = "o custo" }, Options);

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("tradeoff", out var value));
        Assert.Equal("o custo", value.GetString());
    }

    // TR-03: sem valor o campo vai como null — o Ledger trata ausência, não string vazia.
    [Fact]
    public void Project_sem_tradeoff_serializa_o_campo_como_null()
    {
        var json = JsonSerializer.Serialize(new Project { Title = "t" }, Options);

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("tradeoff", out var value));
        Assert.Equal(JsonValueKind.Null, value.ValueKind);
    }
}
