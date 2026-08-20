using System.Text.Json;
using Portfolio.Models;
using Xunit;

namespace Portfolio.Tests;

/// <summary>
/// Contrato público de <see cref="Project"/>. Não toca no Firestore: o que está sob teste é o
/// JSON que o frontend consome, e ele quebra sem aviso do compilador porque não há DTO (C-4).
/// </summary>
public class ProjectContractTests
{
    // A API serializa com as opções padrão do ASP.NET Core (camelCase).
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    // PD-01: o frontend lê `decisions[].title/why/cost`; qualquer outro nome apaga a seção em silêncio.
    [Fact]
    public void Project_serializa_decisions_com_title_why_e_cost_em_camelCase()
    {
        var project = new Project
        {
            Title = "t",
            Decisions = [new Decision { Title = "d", Why = "porque", Cost = "custo" }],
        };

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(project, Options));

        Assert.True(doc.RootElement.TryGetProperty("decisions", out var decisions));
        var first = Assert.Single(decisions.EnumerateArray().ToArray());
        Assert.Equal("d", first.GetProperty("title").GetString());
        Assert.Equal("porque", first.GetProperty("why").GetString());
        Assert.Equal("custo", first.GetProperty("cost").GetString());
    }

    // PD-02: sem decisões o campo sai como array vazio, nunca null — o frontend faz .map direto.
    [Fact]
    public void Project_sem_decisoes_serializa_array_vazio()
    {
        using var doc = JsonDocument.Parse(
            JsonSerializer.Serialize(new Project { Title = "t" }, Options));

        Assert.True(doc.RootElement.TryGetProperty("decisions", out var decisions));
        Assert.Equal(JsonValueKind.Array, decisions.ValueKind);
        Assert.Empty(decisions.EnumerateArray().ToArray());
    }

    // O campo antigo foi absorvido pela lista; deixá-lo no contrato seria duas fontes da verdade.
    [Fact]
    public void Project_nao_expoe_mais_o_campo_tradeoff()
    {
        using var doc = JsonDocument.Parse(
            JsonSerializer.Serialize(new Project { Title = "t" }, Options));

        Assert.False(doc.RootElement.TryGetProperty("tradeoff", out _));
    }
}
