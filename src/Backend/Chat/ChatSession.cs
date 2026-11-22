using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Backend.AI;
using Pi.Options;

namespace Pi.Backend.Chat;

/// <summary>
/// Lógica da conversa: ler entrada, decidir saída, pedir a resposta e exibi-la.
/// Não conhece detalhes do terminal nem escreve no console — tudo passa por IChatView.
/// </summary>
public sealed class ChatSession
{
    private readonly IChatView _view;
    private readonly ProcessMessage _processMessage;
    private readonly ILLMClient _llmClient;
    private readonly AssistantOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ChatSession> _logger;

    public ChatSession(
        IChatView view,
        ProcessMessage processMessage,
        ILLMClient llmClient,
        IOptions<AssistantOptions> options,
        IHostApplicationLifetime lifetime,
        ILogger<ChatSession> logger)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(processMessage);
        ArgumentNullException.ThrowIfNull(llmClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(lifetime);
        ArgumentNullException.ThrowIfNull(logger);

        _view = view;
        _processMessage = processMessage;
        _llmClient = llmClient;
        _options = options.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _view.ShowWelcome(_llmClient.ModelName, _options.AgentName);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? entrada;

            try
            {
                entrada = await _view.ReadInputAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Falha ao ler a entrada do console");
                _lifetime.StopApplication();
                break;
            }

            if (entrada is null)
            {
                _lifetime.StopApplication();
                break;
            }

            if (string.IsNullOrWhiteSpace(entrada))
            {
                continue;
            }

            if (_options.ExitKeywords.Any(keyword =>
                    entrada.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                _lifetime.StopApplication();
                break;
            }

            try
            {
                string resposta;

                await using (_view.BeginThinking(_options.AgentName, _llmClient.ModelName))
                {
                    resposta = await _processMessage.ExecuteAsync(entrada, cancellationToken);
                }

                _view.ShowReply(resposta, _options.AgentName);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");
                _view.ShowError(ex.Message);
            }
        }
    }
}
