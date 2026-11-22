using Pi.Backend.Chat;

namespace Pi.Frontend;

/// <summary>
/// Interface textual mínima: escreve linhas no stdout e lê do stdin.
/// Não interpreta teclas, não usa ANSI e não conhece o terminal — acentos,
/// pontuação e o layout do teclado ficam a cargo do sistema operacional.
/// </summary>
public sealed class ConsoleChatView : IChatView
{
    private readonly TextWriter _output;
    private readonly TextReader _input;

    public ConsoleChatView(TextWriter? output = null, TextReader? input = null)
    {
        _output = output ?? Console.Out;
        _input = input ?? Console.In;
    }

    public void ShowWelcome(string model, string agent)
    {
        _output.WriteLine("Olá! Sou a Pi ✨");
        _output.WriteLine($"Modelo: {model} · Agente: {agent}");
        _output.WriteLine("Digite sua mensagem ou \"sair\" para encerrar.");
        _output.WriteLine();
    }

    public async Task<string?> ReadInputAsync(CancellationToken cancellationToken = default)
    {
        _output.Write("você › ");
        _output.Flush();
        return await _input.ReadLineAsync(cancellationToken);
    }

    public IAsyncDisposable BeginThinking(string agent, string model)
    {
        // Com entrada/saída redirecionada o Enter não é ecoado: fecha a linha
        // do prompt para o status não aparecer colado nele.
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            _output.WriteLine();
        }

        _output.WriteLine($"pensando · {agent} · {model}");
        _output.Flush();
        return NoopDisposable.Instance;
    }

    public void ShowReply(string response, string agent)
    {
        _output.WriteLine(response);
        _output.WriteLine();
    }

    public void ShowError(string message) => _output.WriteLine($"erro: {message}");

    private sealed class NoopDisposable : IAsyncDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
