namespace Pi.Application.Abstractions.Tools;

/// <summary>
/// Uma ferramenta que pode ser acionada pela entrada do usuário.
/// </summary>
public interface ITool
{
    string Name { get; }

    bool CanHandle(string input);

    Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default);
}
