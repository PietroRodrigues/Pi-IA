// Envelope v1: roundtip, validação e leitura tipada do payload.
using System.Text.Json;
using Pi.Contracts.Protocol;
using Xunit;

namespace Pi.Bridge.Tests;

public class ProtocolEnvelopeTests
{
    [Fact]
    public void CreateParse_roundtripPreservaCorrelationIdEPayload()
    {
        var envelope = ProtocolEnvelope.Create(
            EventTypes.StreamDelta,
            new StreamDelta("Olá"),
            "corr-1",
            "event-1");
        var line = envelope.ToJson();

        var parsed = ProtocolEnvelope.Parse(line);

        Assert.Equal(1, parsed.Version);
        Assert.Equal("event-1", parsed.Id);
        Assert.Equal("corr-1", parsed.CorrelationId);
        Assert.Equal(EventTypes.StreamDelta, parsed.Type);
        Assert.Equal("Olá", parsed.Read<StreamDelta>().Text);
    }

    [Fact]
    public void Parse_VersaoDiferente_LancaJsonException()
    {
        const string line = """
            {"v":2,"id":"x","correlationId":"","timestamp":"2026-01-01T00:00:00Z","type":"stream_delta","payload":{"text":"a"}}
            """;

        Assert.Throws<JsonException>(() => ProtocolEnvelope.Parse(line));
    }

    [Fact]
    public void Parse_SemTipo_LancaJsonException()
    {
        const string line = """
            {"v":1,"id":"x","correlationId":"","timestamp":"2026-01-01T00:00:00Z","type":"","payload":{}}
            """;

        Assert.Throws<JsonException>(() => ProtocolEnvelope.Parse(line));
    }

    [Fact]
    public void Create_TimestampEstaEmFormatoIso8601()
    {
        var envelope = ProtocolEnvelope.Create(EventTypes.ContextMetrics, new ContextMetrics(1, 2, 50));

        Assert.True(DateTimeOffset.TryParse(envelope.Timestamp, out _));
        Assert.EndsWith("Z", envelope.Timestamp);
    }
}
