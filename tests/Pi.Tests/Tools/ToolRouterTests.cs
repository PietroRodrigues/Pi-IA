// Garante que o roteador usa a primeira ferramenta que serve e devolve null quando nenhuma serve.
using NSubstitute;
using Pi.Core.Chat;
using Pi.Core.Contracts;
using Pi.Tools;
using Xunit;

namespace Pi.Tests;

public class ToolRouterTests
{
    [Fact]
    public async Task TryExecuteAsync_ReturnsNullWhenNoToolMatches()
    {
        var tool = Substitute.For<ITool>();
        tool.CanHandle(Arg.Any<string>()).Returns(false);

        var result = await new ToolRouter([tool]).TryExecuteAsync("texto qualquer");

        Assert.Null(result);
    }

    [Fact]
    public async Task TryExecuteAsync_UsesTheFirstMatchingTool()
    {
        var first = Substitute.For<ITool>();
        var second = Substitute.For<ITool>();
        first.CanHandle("abra notepad").Returns(true);
        first.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("primeira");
        second.CanHandle("abra notepad").Returns(true);
        second.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("segunda");

        var result = await new ToolRouter([first, second]).TryExecuteAsync("abra notepad");

        Assert.Equal("primeira", result);
        await second.DidNotReceive().ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
