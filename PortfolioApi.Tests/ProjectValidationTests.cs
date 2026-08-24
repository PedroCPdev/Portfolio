using System.ComponentModel.DataAnnotations;
using Portfolio.Models;
using Xunit;

namespace Portfolio.Tests;

/// <summary>
/// Regras de entrada do projeto (ADM-10). A verificação percorre a mesma chamada que o
/// <c>ValidationFilter</c> faz em produção — inclusive a parte que o .NET **não** faz sozinho:
/// descer nos itens de coleção (design D-6, verificado no bloco C da Cadeia de Verificação).
/// </summary>
public class ProjectValidationTests
{
    private static (bool Ok, string Erros) Validar(Project projeto)
    {
        var resultados = new List<ValidationResult>();
        var ok = Validator.TryValidateObject(projeto, new ValidationContext(projeto), resultados, validateAllProperties: true);
        return (ok, string.Join(" ", resultados.Select(r => r.ErrorMessage)));
    }

    private static Project Valido() => new()
    {
        Title = "Portfolio API",
        Description = "API mínima em ASP.NET Core que serve os projetos do portfólio.",
        Tags = ["C#", "Firestore"],
        GithubUrl = "https://github.com/PedroCPdev/Portfolio",
        Decisions = [new Decision { Title = "Firestore no lugar do Postgres", Why = "o free tier não pausa", Cost = "perde SQL" }],
    };

    // ADM-10 (sucesso)
    [Fact]
    public void Projeto_completo_e_dentro_dos_limites_e_valido()
    {
        Assert.True(Validar(Valido()).Ok);
    }

    // ADM-10 (sem sucesso): ausência não é erro — listas vazias e urls nulas são estado legítimo
    [Fact]
    public void Projeto_sem_decisoes_sem_tags_e_sem_links_e_valido()
    {
        var projeto = Valido();
        projeto.Decisions = [];
        projeto.Tags = [];
        projeto.GithubUrl = null;
        projeto.LiveUrl = null;
        projeto.ImageUrl = null;

        Assert.True(Validar(projeto).Ok);
    }

    // ADM-10 (falha): título e descrição são obrigatórios de verdade, espaço em branco não conta
    [Theory]
    [InlineData("", "descrição válida")]
    [InlineData("   ", "descrição válida")]
    [InlineData("título válido", "")]
    [InlineData("título válido", "    ")]
    public void Titulo_ou_descricao_em_branco_invalida(string titulo, string descricao)
    {
        var projeto = Valido();
        projeto.Title = titulo;
        projeto.Description = descricao;

        Assert.False(Validar(projeto).Ok);
    }

    // ADM-10 (falha): limites da spec
    [Fact]
    public void Campos_acima_do_limite_invalidam_e_a_mensagem_nomeia_o_campo()
    {
        var longo = Valido();
        longo.Title = new string('t', 121);
        var (okTitulo, errosTitulo) = Validar(longo);
        Assert.False(okTitulo);
        Assert.Contains("Title", errosTitulo);

        var descricao = Valido();
        descricao.Description = new string('d', 2001);
        Assert.False(Validar(descricao).Ok);
    }

    // ADM-10 (falha): a decisão inválida está DENTRO de um array — é o caso que o
    // TryValidateObject padrão deixa passar
    [Fact]
    public void Decisao_sem_titulo_ou_sem_porque_invalida_o_projeto()
    {
        var semTitulo = Valido();
        semTitulo.Decisions = [new Decision { Title = "", Why = "tem porquê" }];
        Assert.False(Validar(semTitulo).Ok);

        var semPorque = Valido();
        semPorque.Decisions = [new Decision { Title = "tem título", Why = "   " }];
        Assert.False(Validar(semPorque).Ok);

        var semCusto = Valido();
        semCusto.Decisions = [new Decision { Title = "tem título", Why = "tem porquê", Cost = "" }];
        Assert.True(Validar(semCusto).Ok); // custo é opcional (AD-008)
    }

    // ADM-10 (falha): teto de decisões e de tags, e tamanho do item da tag
    [Fact]
    public void Excesso_de_decisoes_ou_tags_invalida()
    {
        var muitasDecisoes = Valido();
        muitasDecisoes.Decisions = Enumerable.Range(0, 11)
            .Select(i => new Decision { Title = $"d{i}", Why = "porque sim" }).ToArray();
        Assert.False(Validar(muitasDecisoes).Ok);

        var muitasTags = Valido();
        muitasTags.Tags = Enumerable.Range(0, 13).Select(i => $"tag{i}").ToArray();
        Assert.False(Validar(muitasTags).Ok);

        var tagLonga = Valido();
        tagLonga.Tags = ["ok", new string('x', 31)];
        Assert.False(Validar(tagLonga).Ok);

        var tagEmBranco = Valido();
        tagEmBranco.Tags = ["ok", "   "];
        Assert.False(Validar(tagEmBranco).Ok);
    }

    // ADM-10 (falha): o esquema fechado em http/https é o que impede um javascript: chegar a um href
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("ftp://exemplo.com/arquivo")]
    [InlineData("nem-url")]
    public void Url_fora_de_http_ou_https_invalida(string url)
    {
        var projeto = Valido();
        projeto.LiveUrl = url;

        Assert.False(Validar(projeto).Ok);
    }

    [Theory]
    [InlineData("http://exemplo.com")]
    [InlineData("https://exemplo.com/caminho?q=1")]
    public void Url_http_ou_https_e_valida(string url)
    {
        var projeto = Valido();
        projeto.LiveUrl = url;

        Assert.True(Validar(projeto).Ok);
    }
}
