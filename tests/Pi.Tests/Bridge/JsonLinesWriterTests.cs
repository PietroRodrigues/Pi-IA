// Garante que cada evento vire exatamente uma linha NDJSON, com os campos certos.
using System.Text;
using System.Text.Json;
using Pi.Bridge;
using Xunit;

namespace Pi.Tests.Bridge;

public class JsonLinesWriterTests
{
    [Fact]
    public async Task WriteAsync_EmiteUmaLinhaJsonPorEvento()
    {
        var buffer = new StringBuilder();
        using var output = new StringWriter(buffer);
        var sut = new JsonLinesWriter(output);

        await sut.WriteAsync(new TokenEvent("r1", "Olá"));
        await sut.WriteAsync(new DoneEvent("r1", "Olá", false));

        var lines = buffer.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Equal(2, lines.Length);

        using var first = JsonDocument.Parse(lines[0]);
        Assert.Equal("token", first.RootElement.GetProperty("type").GetString());
        Assert.Equal("r1", first.RootElement.GetProperty("requestId").GetString());
        Assert.Equal("Olá", first.RootElement.GetProperty("text").GetString());

        using var second = JsonDocument.Parse(lines[1]);
        Assert.Equal("done", second.RootElement.GetProperty("type").GetString());
        Assert.False(second.RootElement.GetProperty("cancelled").GetBoolean());
    }
}
