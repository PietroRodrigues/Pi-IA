using Microsoft.Extensions.Options;
using Pi.Abstracts.AI;
using Pi.Options;

namespace Pi.Workers;

/// <summary>
/// Formata o texto de interface do chat: boas-vindas, indicador de pensamento
/// e respostas. Não escreve no console — apenas devolve strings, o que mantém
/// a apresentação fácil de testar e separada dos workers.
/// </summary>
public sealed class ChatPresenter
{
    private readonly ILLMClient _llmClient;
    private readonly AssistantOptions _options;

    public ChatPresenter(ILLMClient llmClient, IOptions<AssistantOptions> options)
    {
        ArgumentNullException.ThrowIfNull(llmClient);
        ArgumentNullException.ThrowIfNull(options);

        _llmClient = llmClient;
        _options = options.Value;
    }

    /// <summary>
    /// Banner exibido uma vez, quando o chat começa a aceitar mensagens.
    /// </summary>
    public string Welcome() =>
        "Olá! Sou a Pi ✨\n" +
        $"Modelo: {_llmClient.ModelName} · Agente: {_options.AgentName}\n" +
        "Digite sua mensagem (ou \"sair\" para encerrar).\n";

    /// <summary>
    /// Texto base do indicador exibido enquanto a resposta é gerada.
    /// </summary>
    public string Thinking() =>
        $"💭 Pi está pensando... ({_options.AgentName} · {_llmClient.ModelName})";

    /// <summary>
    /// Resposta do assistente, no formato já usado pelo CLI.
    /// </summary>
    public string Reply(string response) => $"\nPi: {response}";
}
