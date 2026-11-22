// Uma ferramenta simples: reconhece a entrada e devolve um texto.
namespace Pi.Core.Contracts;

// Uma ferramenta que pode ser acionada pela entrada do usuário.
public interface ITool
{
    string Name { get; }

    bool CanHandle(string input);

    Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default);
}
