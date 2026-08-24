using System.ComponentModel.DataAnnotations;

namespace Portfolio.Validation;

/// <summary>
/// Executa as DataAnnotations do corpo antes de o endpoint rodar. Minimal API do .NET 9 **não**
/// faz isso sozinha — sem este filtro os atributos dos models seriam decoração (design D-5).
///
/// A resposta segue o shape da casa, <c>{ success, error }</c>, o mesmo de
/// <c>ContactEndpoints</c> e o que o cliente do painel sabe ler.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var alvo = context.Arguments.OfType<T>().FirstOrDefault();
        if (alvo is null) return Erro("Request body is required.");

        var resultados = new List<ValidationResult>();
        if (!Validator.TryValidateObject(alvo, new ValidationContext(alvo), resultados, validateAllProperties: true))
            return Erro(string.Join(" ", resultados.Select(r => r.ErrorMessage)));

        return await next(context);
    }

    private static IResult Erro(string mensagem) =>
        Results.Json(new { success = false, error = mensagem }, statusCode: StatusCodes.Status400BadRequest);
}
