namespace Pi.Workers;

/// <summary>
/// Indicador temporário de "pensando". Em um terminal interativo anima um
/// spinner sobre a mesma linha; com a saída redirecionada (pipes, arquivos,
/// ferramentas) escreve uma única linha estática, sem códigos de controle.
/// </summary>
public sealed class ThinkingIndicator : IAsyncDisposable
{
    private static readonly string[] Frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private static readonly TimeSpan DefaultFrameInterval = TimeSpan.FromMilliseconds(120);

    private readonly TextWriter _writer;
    private readonly string _message;
    private readonly TimeSpan _frameInterval;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task? _animation;
    private bool _disposed;

    /// <param name="message">Texto exibido à esquerda do spinner.</param>
    /// <param name="writer">Destino da saída.</param>
    /// <param name="animate">
    /// Força animação (<c>true</c>) ou texto estático (<c>false</c>). Por padrão,
    /// anima somente quando <see cref="Console.IsOutputRedirected"/> é falso.
    /// </param>
    /// <param name="frameInterval">Intervalo entre frames; ajustável nos testes.</param>
    public ThinkingIndicator(
        string message,
        TextWriter writer,
        bool? animate = null,
        TimeSpan? frameInterval = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(writer);

        _message = message;
        _writer = writer;
        _frameInterval = frameInterval ?? DefaultFrameInterval;

        if (animate ?? !Console.IsOutputRedirected)
        {
            // Primeiro frame já no construtor: o usuário vê o indicator na hora,
            // sem esperar o primeiro tick — e os testes não dependem de timing.
            EscreverFrame(0);

            _animation = Task.Run(() => AnimarAsync(_cancellation.Token));
        }
        else
        {
            writer.WriteLine(message);
            writer.Flush();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_animation is not null)
        {
            await _cancellation.CancelAsync();

            try
            {
                await _animation;
            }
            catch (OperationCanceledException)
            {
                // Esperado: o loop termina quando cancelamos.
            }

            // Apaga a linha antes de liberar o cursor para a resposta.
            var limpeza = new string(' ', _message.Length + 3);
            await _writer.WriteAsync($"\r{limpeza}\r");
            await _writer.FlushAsync();
        }

        _cancellation.Dispose();
    }

    private async Task AnimarAsync(CancellationToken cancellationToken)
    {
        var indice = 1;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_frameInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            EscreverFrame(indice % Frames.Length);
            indice++;
        }
    }

    private void EscreverFrame(int indice)
    {
        _writer.Write($"\r{_message} {Frames[indice]}");
        _writer.Flush();
    }
}
