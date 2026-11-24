// Persistência de histórico e checkpoints de sessão, sem depender de I/O no domínio.
using Pi.Contracts.Models;

namespace Pi.Core.Abstractions;

public interface ISessionRepository
{
    Task<SessionSnapshot?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task SaveAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default);
}
