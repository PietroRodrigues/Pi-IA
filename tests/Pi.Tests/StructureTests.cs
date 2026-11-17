using System.Reflection;
using Xunit;

namespace Pi.Tests;

/// <summary>
/// A estrutura é plana e os módulos são pastas. Estes testes preservam duas
/// regras que antes eram impostas por projetos separados: as interfaces em
/// Abstracts não conhecem implementações, e Conversation permanece isolada.
/// </summary>
public sealed class StructureTests
{
    private static readonly string[] ConcreteNamespaces =
    [
        "Pi.Ollama",
        "Pi.Messaging",
        "Pi.Tools",
        "Pi.Workers",
        "Microsoft.Extensions"
    ];

    [Fact]
    public void Abstracts_NaoDeveReferenciarImplementacoesConcretas()
    {
        var arquivos = ArquivosDe("Abstracts");

        var violacoes = violacoesEm(arquivos, ConcreteNamespaces);

        Assert.True(
            violacoes.Length == 0,
            "Pi.Abstracts deve conter apenas contratos. Violações:\n" + string.Join("\n", violacoes));
    }

    [Fact]
    public void Conversation_NaoDeveReferenciarOutrosModulos()
    {
        string[] proibidos = ["Pi.Abstracts", .. ConcreteNamespaces, "Pi.Services", "Pi.UseCases", "Pi.Models", "Pi.Options"];
        var arquivos = ArquivosDe("Conversation");

        var violacoes = violacoesEm(arquivos, proibidos);

        Assert.True(
            violacoes.Length == 0,
            "Pi.Conversation deve permanecer independente. Violações:\n" + string.Join("\n", violacoes));
    }

    [Fact]
    public void Abstracts_NaoDeveExporTiposConcretosNaApiPublica()
    {
        var tiposAbstratos = typeof(global::Pi.Abstracts.AI.ILLMClient).Assembly.GetTypes()
            .Where(tipo => tipo.Namespace?.StartsWith("Pi.Abstracts", StringComparison.Ordinal) == true);

        var violacoes = new List<string>();

        foreach (var tipo in tiposAbstratos)
        {
            var membros = tipo.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (var membro in membros)
            {
                var tiposReferenciados = new List<Type>();

                switch (membro)
                {
                    case PropertyInfo propriedade:
                        tiposReferenciados.Add(propriedade.PropertyType);
                        break;
                    case FieldInfo campo:
                        tiposReferenciados.Add(campo.FieldType);
                        break;
                    case MethodInfo metodo:
                        tiposReferenciados.Add(metodo.ReturnType);
                        tiposReferenciados.AddRange(
                            metodo.GetParameters().Select(parametro => parametro.ParameterType));
                        break;
                    case ConstructorInfo construtor:
                        tiposReferenciados.AddRange(
                            construtor.GetParameters().Select(parametro => parametro.ParameterType));
                        break;
                }

                foreach (var referenciado in tiposReferenciados)
                {
                    if (ConcreteNamespaces.Any(ns =>
                            referenciado.Namespace?.StartsWith(ns, StringComparison.Ordinal) == true))
                    {
                        violacoes.Add($"{tipo.FullName}.{membro.Name} -> {referenciado.FullName}");
                    }
                }
            }
        }

        Assert.True(
            violacoes.Count == 0,
            "A API pública de Pi.Abstracts não pode expor implementações concretas:\n" + string.Join("\n", violacoes));
    }

    private static string[] ArquivosDe(string modulo)
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (diretorio is not null)
        {
            var candidato = Path.Combine(diretorio.FullName, "src", modulo);
            if (Directory.Exists(candidato))
            {
                return Directory.GetFiles(candidato, "*.cs", SearchOption.AllDirectories);
            }

            diretorio = diretorio.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Não foi possível localizar src/{modulo} a partir de {AppContext.BaseDirectory}.");
    }

    private static string[] violacoesEm(IEnumerable<string> arquivos, IEnumerable<string> namespacesProibidos)
    {
        return arquivos
            .SelectMany(arquivo => File.ReadAllLines(arquivo)
                .Select((linha, indice) => new
                {
                    Arquivo = Path.GetFileName(arquivo),
                    Numero = indice + 1,
                    Texto = linha
                }))
            .Where(item => namespacesProibidos.Any(ns => item.Texto.Contains(ns, StringComparison.Ordinal)))
            .Select(item => $"{item.Arquivo}:{item.Numero} -> {item.Texto.Trim()}")
            .ToArray();
    }
}
