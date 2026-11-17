using Pi.Workers;
using Xunit;

namespace Pi.Tests;

public sealed class ThinkingIndicatorTests
{
    [Fact]
    public async Task SaidaRedirecionada_EscreveUmaUnicaLinhaEstatica()
    {
        using var writer = new StringWriter();
        await using var indicator = new ThinkingIndicator("pensando", writer, animate: false);

        Assert.Equal("pensando" + Environment.NewLine, writer.ToString());
    }

    [Fact]
    public async Task TerminalInterativo_AnimaSpinnerELimpaLinhaAoDispor()
    {
        using var writer = new StringWriter();

        var indicator = new ThinkingIndicator(
            "pensando",
            writer,
            animate: true,
            frameInterval: TimeSpan.FromMilliseconds(1));

        var durante = writer.ToString();
        Assert.Contains("\r", durante);
        Assert.Contains("⠋", durante);

        await indicator.DisposeAsync();

        Assert.EndsWith(new string(' ', "pensando".Length + 3) + "\r", writer.ToString());
    }
}
