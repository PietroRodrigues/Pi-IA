// Leitura linha a linha com limite de tamanho.
using System.Text;
using Pi.Host.Bridge;
using Xunit;

namespace Pi.Bridge.Tests;

public class NdjsonReaderTests
{
    [Fact]
    public async Task ReadLineAsync_LeDuasLinhasEFecha()
    {
        using var input = new StringReader("linha 1\nlinha 2\n");
        var sut = new NdjsonReader(input);

        Assert.Equal("linha 1", await sut.ReadLineAsync());
        Assert.Equal("linha 2", await sut.ReadLineAsync());
        Assert.Null(await sut.ReadLineAsync());
    }

    [Fact]
    public async Task ReadLineAsync_RemoveCrNoFim()
    {
        using var input = new StringReader("linha\r\n");
        var sut = new NdjsonReader(input);

        Assert.Equal("linha", await sut.ReadLineAsync());
    }

    [Fact]
    public async Task ReadLineAsync_LinhaAcimaDoLimite_LancaInvalidDataException()
    {
        using var input = new StringReader(new string('a', 100));
        var sut = new NdjsonReader(input, maxLineLength: 10);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await sut.ReadLineAsync());
    }

    [Fact]
    public async Task ReadLineAsync_UltimaLinhaSemNewline_DevolveConteudo()
    {
        using var input = new StringReader("final");
        var sut = new NdjsonReader(input);

        Assert.Equal("final", await sut.ReadLineAsync());
        Assert.Null(await sut.ReadLineAsync());
    }
}
