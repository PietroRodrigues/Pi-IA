// Estado resumido da sessão: identificação, histórico e uso da janela de contexto.
using Pi.Contracts.Constants;

namespace Pi.Contracts.Models;

public sealed record ContextUsage(int Used, int Limit, double Percent);

public sealed record SessionMessage(string Role, string Text, string Timestamp)
{
    public static SessionMessage Create(string role, string text) =>
        new(role, text, DateTimeOffset.UtcNow.ToString(PiConstants.Formats.TimestampRoundtrip));
}

public sealed record SessionSnapshot(
    string Id,
    string Model,
    string Agent,
    string Cwd,
    string Branch,
    List<SessionMessage> Messages,
    ContextUsage Context,
    string UpdatedAt)
{
    public static SessionSnapshot Create(
        string id,
        string model,
        string agent,
        string cwd,
        string branch,
        int limit = PiConstants.Defaults.ContextWindowTokens) =>
        new(
            id,
            model,
            agent,
            cwd,
            branch,
            [],
            new ContextUsage(0, limit, 0),
            DateTimeOffset.UtcNow.ToString(PiConstants.Formats.TimestampRoundtrip));
}
