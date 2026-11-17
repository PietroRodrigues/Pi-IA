namespace Pi.UI;

/// <summary>
/// Contrato da interface do chat. A lógica conhece apenas este contrato:
/// a implementação decide como desenhar no terminal e como ler a entrada.
/// </summary>
public interface IChatView
{
    void ShowWelcome(string model, string agent);

    /// <summary>
    /// Lê a próxima linha digitada. Retorna <c>null</c> em EOF.
    /// </summary>
    Task<string?> ReadInputAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia o indicador de "pensando". Deve ser descartado antes da resposta.
    /// </summary>
    IAsyncDisposable BeginThinking(string agent, string model);

    void ShowReply(string response, string agent);

    void ShowError(string message);
}
