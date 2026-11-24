// Parser de comandos de texto das ferramentas.
using Pi.Infrastructure.Tools;
using Xunit;

namespace Pi.Infrastructure.Tests;

public class ToolInputTests
{
    [Fact]
    public void Argument_RetornaTextoAposPrimeiraPalavra()
    {
        Assert.Equal("arquivo.txt 10 20", ToolInput.Argument("leia arquivo.txt 10 20"));
        Assert.Equal(string.Empty, ToolInput.Argument("leia"));
    }

    [Fact]
    public void Tokens_RespeitaAspasDuplas()
    {
        var tokens = ToolInput.Tokens("\"arquivo com espaço.txt\" 10");

        Assert.Equal(["arquivo com espaço.txt", "10"], tokens);
    }
}
