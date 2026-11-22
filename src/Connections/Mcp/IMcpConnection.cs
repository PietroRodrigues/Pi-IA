// Espaço reservado para conexões MCP. Hoje é só o contrato, sem transporte.
using Pi.Core.Contracts;

namespace Pi.Connections.Mcp;

// Ponto de extensão para futuras conexões MCP (locais ou remotas). Uma
// implementação expõe ferramentas usando o contrato que o Core já conhece;
// nenhum pacote ou transporte MCP é necessário agora.
public interface IMcpConnection
{
    string Name { get; }

    Task<IReadOnlyList<ITool>> GetToolsAsync(CancellationToken cancellationToken = default);
}
