using Microsoft.Extensions.Options;

namespace Portfolio.Services;

public class ContactOptions
{
    /// <summary>
    /// Metade da cota grátis do Resend (100/dia). O contador mora em memória e zera quando a
    /// instância reinicia; a outra metade é a folga para um reinício no meio do dia.
    /// </summary>
    public int DailyEmailLimit { get; set; } = 50;
}

/// <summary>
/// Teto de e-mails do contato por dia UTC (C-10). Sem ele, um robô que respeite o rate limit do
/// C-6 esgota a cota do Resend em menos de uma hora e nenhuma mensagem legítima sai até o dia
/// seguinte. Conta só o que vai de fato para o Resend: o endpoint consome depois da validação,
/// do honeypot e da checagem de configuração.
/// </summary>
public class DailyEmailBudget(IOptions<ContactOptions> options, TimeProvider time)
{
    private readonly int _limit = options.Value.DailyEmailLimit;
    private readonly Lock _lock = new();
    private DateOnly _day;
    private int _used;

    public bool TryConsume()
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        lock (_lock)
        {
            if (today != _day)
            {
                _day = today;
                _used = 0;
            }
            if (_used >= _limit) return false;
            _used++;
            return true;
        }
    }
}
