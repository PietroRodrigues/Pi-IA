using Pi.Connections.Ollama;
using Xunit;

namespace Pi.Tests;

public class OllamaResponseParserTests
{
    [Fact]
    public void Parse_ReadsResponseFromSingleJson()
    {
        const string body = """
            {"model":"qwen2.5:3b","response":"Olá, tudo bem?","done":true}
            """;

        var result = OllamaResponseParser.Parse(body);

        Assert.Equal("Olá, tudo bem?", result);
    }

    [Fact]
    public void Parse_ReadsFirstResponseFromNdjson()
    {
        const string body = """
            {"response":"primeira"}
            {"response":"segunda"}
            """;

        var result = OllamaResponseParser.Parse(body);

        Assert.Equal("primeira", result);
    }

    [Fact]
    public void Parse_ThrowsWhenResponseFieldIsMissing()
    {
        const string body = """{"done":true}""";

        Assert.Throws<InvalidOperationException>(() => OllamaResponseParser.Parse(body));
    }

    [Fact]
    public void Parse_ThrowsForEmptyBody()
        => Assert.Throws<InvalidOperationException>(() => OllamaResponseParser.Parse(string.Empty));
}
