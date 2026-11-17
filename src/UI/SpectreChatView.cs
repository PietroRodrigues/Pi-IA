using Spectre.Console;

namespace Pi.UI;

/// <summary>
/// Implementação da interface do chat com Spectre.Console: painéis, cores,
/// prompt estilizado e spinner. Quando a saída é redirecionada, o Spectre
/// desliga ANSI e animação automaticamente, deixando texto legível.
/// </summary>
public sealed class SpectreChatView : IChatView
{
    private readonly IAnsiConsole _console;
    private readonly TextReader _input;

    public SpectreChatView(IAnsiConsole console, TextReader? input = null)
    {
        ArgumentNullException.ThrowIfNull(console);

        _console = console;
        _input = input ?? Console.In;
    }

    public void ShowWelcome(string model, string agent)
    {
        var corpo = new Markup(
            $"Olá! Sou a [{ChatTheme.Assistant}]Pi[/] ✨\n" +
            $"Modelo: [{ChatTheme.Meta}]{Markup.Escape(model)}[/] · " +
            $"Agente: [{ChatTheme.Meta}]{Markup.Escape(agent)}[/]\n" +
            $"[dim]Digite sua mensagem ou \"sair\" para encerrar.[/]");

        _console.Write(
            new Panel(corpo)
                .Header(new PanelHeader($"[bold {ChatTheme.Accent}]Pi-IA[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0));

        _console.WriteLine();
    }

    public async Task<string?> ReadInputAsync(CancellationToken cancellationToken = default)
    {
        _console.Markup($"[{ChatTheme.User}]você › [/]");
        return await _input.ReadLineAsync(cancellationToken);
    }

    public IAsyncDisposable BeginThinking(string agent, string model)
    {
        var rotulo =
            $"[cyan]💭 pensando[/] [{ChatTheme.Meta}]· {Markup.Escape(agent)} · {Markup.Escape(model)}[/]";

        // Em pipe/arquivo o Enter não é visível: quebra a linha para o status
        // não aparecer colado no prompt.
        if (Console.IsOutputRedirected)
        {
            _console.WriteLine();
        }

        return new ThinkingStatus(_console, rotulo);
    }

    public void ShowReply(string response, string agent)
    {
        var painel = new Panel(new Markup(Markup.Escape(response)))
            .Header(new PanelHeader($"[{ChatTheme.Assistant}]Pi · {Markup.Escape(agent)}[/]"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        _console.WriteLine();
        _console.Write(painel);
    }

    public void ShowError(string message) =>
        _console.MarkupLine($"[{ChatTheme.Error}]⚠ {Markup.Escape(message)}[/]");

    /// <summary>
    /// Mantém o status do Spectre vivo até o chamador descartar o indicador.
    /// </summary>
    private sealed class ThinkingStatus : IAsyncDisposable
    {
        private readonly TaskCompletionSource _parar = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task _tarefa;

        public ThinkingStatus(IAnsiConsole console, string rotulo)
        {
            _tarefa = Task.Run(() => console.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync(rotulo, async _ => await _parar.Task));
        }

        public async ValueTask DisposeAsync()
        {
            _parar.TrySetResult();
            await _tarefa;
        }
    }
}
