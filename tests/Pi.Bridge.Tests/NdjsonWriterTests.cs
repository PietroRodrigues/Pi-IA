// O stdout do host contém exatamente uma linha JSON por evento.
using System.Text;
using Pi.Contracts.Protocol;
using Pi.Host.Bridge;
using Xunit;

namespace Pi.Bridge.Tests;

public class NdjsonWriterTests
{
    [Fact]
    public async Task WriteAsync_EmiteUmaLinhaPorEvento()
    {
        var buffer = new StringBuilder();
        using var output = new StringWriter(buffer);
        var sut = new NdjsonWriter(output);

        await sut.WriteAsync(EventTypes.StreamDelta, new StreamDelta("Olá"), "r1");
        await sut.WriteAsync(EventTypes.StreamDelta, new StreamDelta(string.Empty, Final: true), "r1");

        var lines = buffer.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Equal(2, lines.Length);
        Assert.Contains("\"v\":1", lines[0], StringComparison.Ordinal);
        Assert.Contains("\"correlationId\":\"r1\"", lines[0], StringComparison.Ordinal);
        Assert.Contains("\"final\":true", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteEnvelopeAsync_PreservaEnvelopeRecebido()
    {
        var buffer = new StringBuilder();
        using var output = new StringWriter(buffer);
        var sut = new NdjsonWriter(output);
        var envelope = ProtocolEnvelope.Create(EventTypes.ContextMetrics, new ContextMetrics(1, 32768, 0));

        await sut.WriteEnvelopeAsync(envelope);

        var parsed = ProtocolEnvelope.Parse(buffer.ToString().Trim());
        Assert.Equal(envelope.Id, parsed.Id);
        Assert.Equal(EventTypes.ContextMetrics, parsed.Type);
    }

    [Fact]
    public async Task NdjsonEventSink_RepassaParaEscritor()
    {
        var buffer = new StringBuilder();
        using var output = new StringWriter(buffer);
        var sink = new NdjsonEventSink(new NdjsonWriter(output));

        await sink.PublishAsync(EventTypes.ErrorEvent, new ErrorInfo(ErrorCodes.InvalidMessage, "ruim"), "r9");

        Assert.Contains("invalid_message", buffer.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"correlationId\":\"r9\"", buffer.ToString(), StringComparison.Ordinal);
    }
}
