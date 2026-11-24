// O Ollama pode responder JSON único ou NDJSON; o parser aceita os dois.
using Pi.Infrastructure.Llm;
using Xunit;

namespace Pi.Infrastructure.Tests;

public class OllamaResponseParserTests
{
    [Fact]
    public void Parse_JsonUnico_ExtraiResponse()
    {
        var text = OllamaResponseParser.Parse("""{"model":"qwen","response":"Olá","done":true}""");

        Assert.Equal("Olá", text);
    }

    [Fact]
    public void Parse_Ndjson_UsaPrimeiraLinhaComResponse()
    {
        var body = """
                   {"model":"qwen","response":"","done":false}
                   {"model":"qwen","response":"mundo","done":true}
                   """;

        Assert.Equal("mundo", OllamaResponseParser.Parse(body));
    }

    [Fact]
    public void Parse_SemCampoResponse_Lanca()
    {
        Assert.Throws<InvalidOperationException>(() => OllamaResponseParser.Parse("{}"));
        Assert.Throws<InvalidOperationException>(() => OllamaResponseParser.Parse("   "));
    }
}
