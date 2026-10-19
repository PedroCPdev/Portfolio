using Microsoft.Extensions.Options;
using Portfolio.Services;
using Xunit;

namespace Portfolio.Tests;

/// <summary>Teto diário de e-mails do contato (C-10), sem HTTP e com o relógio na mão do teste.</summary>
public class DailyEmailBudgetTests
{
    private sealed class Relogio(DateTimeOffset agora) : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = agora;
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private static DailyEmailBudget Budget(int limite, TimeProvider relogio) =>
        new(Options.Create(new ContactOptions { DailyEmailLimit = limite }), relogio);

    // AS-03
    [Fact]
    public void Libera_ate_o_limite_e_recusa_o_seguinte()
    {
        var budget = Budget(2, new Relogio(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)));

        Assert.True(budget.TryConsume());
        Assert.True(budget.TryConsume());
        Assert.False(budget.TryConsume());
    }

    // AS-04
    [Fact]
    public void Teto_volta_a_zero_quando_o_dia_utc_vira()
    {
        var relogio = new Relogio(new DateTimeOffset(2026, 10, 19, 23, 59, 0, TimeSpan.Zero));
        var budget = Budget(1, relogio);
        Assert.True(budget.TryConsume());
        Assert.False(budget.TryConsume());

        relogio.Agora = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);

        Assert.True(budget.TryConsume());
    }

    // AS-04: o dia é o UTC. 23:30 em Brasília (UTC-3) já é o dia seguinte em UTC.
    [Fact]
    public void O_dia_e_contado_em_utc_e_nao_no_fuso_do_relogio()
    {
        var relogio = new Relogio(new DateTimeOffset(2026, 10, 19, 20, 0, 0, TimeSpan.FromHours(-3)));
        var budget = Budget(1, relogio);
        Assert.True(budget.TryConsume());

        relogio.Agora = new DateTimeOffset(2026, 10, 19, 21, 30, 0, TimeSpan.FromHours(-3));

        Assert.True(budget.TryConsume());
    }
}
