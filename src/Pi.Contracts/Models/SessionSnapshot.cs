// Estado resumido da sessão: identificação, histórico e uso da janela de contexto.
using System.Text.Json.Serialization;
using Pi.Contracts.Constants;

namespace Pi.Contracts.Models;

public sealed record ContextUsage(int Used, int Limit, double Percent);

public sealed record SessionToolCall(
    [property: JsonPropertyName(PiConstants.Json.Id)] string Id,
    [property: JsonPropertyName(PiConstants.Json.Name)] string Name,
    [property: JsonPropertyName(PiConstants.Json.Arguments)] string ArgumentsJson);

public sealed record SessionMessage(
    string Role,
    string Text,
    string Timestamp,
    [property: JsonPropertyName(PiConstants.Json.ToolCalls)] IReadOnlyList<SessionToolCall>? ToolCalls = null,
    [property: JsonPropertyName(PiConstants.Json.ToolCallId)] string? ToolCallId = null)
{
    public static SessionMessage Create(
        string role,
        string text,
        IReadOnlyList<SessionToolCall>? toolCalls = null,
        string? toolCallId = null) =>
        new(role, text, DateTimeOffset.UtcNow.ToString(PiConstants.Formats.TimestampRoundtrip), toolCalls, toolCallId);
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
