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

    [Fact]
    public async Task ReadLineAsync_LinhaLongaComNewlineNoMesmoChunk_LancaInvalidDataException()
    {
        using var input = new StringReader($"{new string('a', 100)}\n");
        var sut = new NdjsonReader(input, maxLineLength: 10);

        var error = await Assert.ThrowsAsync<InvalidDataException>(async () => await sut.ReadLineAsync());

        Assert.Contains("bytes", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadLineAsync_MultibyteAcimaDoLimiteEmBytes_LancaInvalidDataException()
    {
        using var input = new StringReader($"{new string('\u00E9', 6)}\n");
        var sut = new NdjsonReader(input, maxLineLength: 10);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await sut.ReadLineAsync());
    }

    [Fact]
    public async Task ReadLineAsync_DuasLinhasMultibyteNoMesmoChunk_NaoAcumulaBytes()
    {
        var line = new string('\u00E9', 5);
        using var input = new StringReader($"{line}\n{line}\n");
        var sut = new NdjsonReader(input, maxLineLength: 10);

        Assert.Equal(line, await sut.ReadLineAsync());
        Assert.Equal(line, await sut.ReadLineAsync());
        Assert.Null(await sut.ReadLineAsync());
    }

    [Fact]
    public async Task ReadLineAsync_UltimaLinhaSemNewlineAcimaDoLimiteEmBytes_LancaInvalidDataException()
    {
        using var input = new StringReader(new string('\u00E9', 6));
        var sut = new NdjsonReader(input, maxLineLength: 10);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await sut.ReadLineAsync());
    }
}
